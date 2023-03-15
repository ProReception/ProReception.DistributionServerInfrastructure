namespace ProReception.DistributionServerInfrastructure.Settings;

using System.Text.Json;
using AuthenticatedEncryption;
using Models;
using Models.Internal;
using Models.Public;

public abstract class SettingsManagerBase<T> : ISettingsManagerBase where T : BaseSettings, new()
{
    private readonly byte[] _cryptKey;
    private readonly byte[] _authKey;
    private readonly string _settingsPath;
    private readonly string _settingsFilePath;
    private readonly string _logsPath;
    private readonly T _settings;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    protected SettingsManagerBase(string appName, string cryptKey, string authKey)
    {
        _cryptKey = Convert.FromBase64String(cryptKey);
        _authKey = Convert.FromBase64String(authKey);
        _settingsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Pro Reception\\{appName}";
        _settingsFilePath = $"{_settingsPath}\\settings";
        _logsPath = $"{_settingsPath}\\Logs";
        _settings = Load();
    }

    public TokensRecord? GetTokens()
        => _settings.ProReceptionTokens != null
            ? new TokensRecord(_settings.ProReceptionTokens.AccessToken, _settings.ProReceptionTokens.RefreshToken, _settings.ProReceptionTokens.ExpiresAtUtc)
            : null;

    public async Task SaveTokens(string accessToken, string refreshToken, DateTime expiresAtUtc)
        => await ThreadSafeUpdate(settings =>
            settings.ProReceptionTokens = new Tokens
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAtUtc = expiresAtUtc
            });

    public async Task RemoveTokens()
        => await ThreadSafeUpdate(settings => settings.ProReceptionTokens = null);

    public string GetLogFilesPath()
    {
        if (!Directory.Exists(_logsPath))
        {
            Directory.CreateDirectory(_logsPath);
        }

        return _logsPath;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected async Task ThreadSafeUpdate(Action<T> updateAction)
    {
        await _semaphoreSlim.WaitAsync();
        updateAction(_settings);
        await Save();
        _semaphoreSlim.Release();
    }

    private async Task Save()
    {
        await using var settingsFileStream = new FileStream(_settingsFilePath, FileMode.Create);
        await using var streamWriter = new StreamWriter(settingsFileStream);
        await streamWriter.WriteAsync(Encryption.Encrypt(JsonSerializer.Serialize(_settings), _cryptKey, _authKey));
    }

    private T Load()
    {
        if (!Directory.Exists(_settingsPath))
        {
            Directory.CreateDirectory(_settingsPath);
        }

        if (!File.Exists(_settingsFilePath))
        {
            return new T();
        }

        using var settingsFileStream = new FileStream(_settingsFilePath, FileMode.Open);
        using var streamReader = new StreamReader(settingsFileStream);

        var settings = streamReader.ReadToEnd();
        var decryptedSettings = !string.IsNullOrWhiteSpace(settings)
            ? Encryption.Decrypt(settings, _cryptKey, _authKey)
            : null;

        return !string.IsNullOrWhiteSpace(decryptedSettings)
            ? JsonSerializer.Deserialize<T>(decryptedSettings) ?? new T()
            : new T();
    }
}
