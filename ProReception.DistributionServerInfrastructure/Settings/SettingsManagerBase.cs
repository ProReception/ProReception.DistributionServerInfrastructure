namespace ProReception.DistributionServerInfrastructure.Settings;

using System.Diagnostics;
using System.Text.Json;
using AuthenticatedEncryption;
using JetBrains.Annotations;
using Serilog;
using Models;
using Models.Internal;
using Models.Public;

[PublicAPI]
public abstract class SettingsManagerBase<T> : ISettingsManagerBase where T : BaseSettings, new()
{
    private readonly byte[] _cryptKey;
    private readonly byte[] _authKey;
    private readonly string _settingsFilePath;
    private readonly string _logsPath;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private Guid? _debugOverrideDistributionServerAppId;

    protected SettingsManagerBase(string appName, string cryptKey, string authKey)
    {
        SettingsDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Pro Reception\\{appName}";

        _cryptKey = Convert.FromBase64String(cryptKey);
        _authKey = Convert.FromBase64String(authKey);
        _settingsFilePath = $"{SettingsDirectory}\\settings";
        _logsPath = $"{SettingsDirectory}\\Logs";

        Settings = Load();
    }

    protected string SettingsDirectory { get; }
    protected T Settings { get; }

    public Guid GetDistributionServerAppId() => _debugOverrideDistributionServerAppId ?? Settings.DistributionServerAppId;

    [Conditional("DEBUG")]
    public void SetDebugOverrideDistributionServerAppId(Guid debugOverrideDistributionServerAppId)
        => _debugOverrideDistributionServerAppId = debugOverrideDistributionServerAppId;

    public TokensRecord? GetTokens()
        => Settings.ProReceptionTokens != null
            ? new TokensRecord(Settings.ProReceptionTokens.AccessToken, Settings.ProReceptionTokens.RefreshToken, Settings.ProReceptionTokens.ExpiresAtUtc)
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

    protected async Task ThreadSafeUpdate(Action<T> updateAction)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            updateAction(Settings);
            await Save();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task Save()
    {
        var tmpPath = _settingsFilePath + ".tmp";

        await using (var settingsFileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (var streamWriter = new StreamWriter(settingsFileStream))
        {
            await streamWriter.WriteAsync(Encryption.Encrypt(JsonSerializer.Serialize(Settings), _cryptKey, _authKey));
            await streamWriter.FlushAsync();
            await settingsFileStream.FlushAsync();
        }

        File.Move(tmpPath, _settingsFilePath, overwrite: true);
    }

    /// <summary>
    /// Called when the user logs out. Override to clear site-specific settings.
    /// Default implementation does nothing.
    /// </summary>
    public virtual Task OnUserLoggedOutAsync() => Task.CompletedTask;

    private T Load()
    {
        if (!Directory.Exists(SettingsDirectory))
        {
            Directory.CreateDirectory(SettingsDirectory);
        }

        var tmpPath = _settingsFilePath + ".tmp";

        if (!File.Exists(_settingsFilePath))
        {
            // Attempt recovery from .tmp file if main file is missing
            if (File.Exists(tmpPath))
            {
                Log.Warning("Settings file missing but .tmp file found — recovering from {TmpPath}", tmpPath);
                try
                {
                    File.Move(tmpPath, _settingsFilePath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to recover settings from .tmp file");
                    return new T();
                }
            }
            else
            {
                return new T();
            }
        }

        var settings = File.ReadAllText(_settingsFilePath);

        if (string.IsNullOrWhiteSpace(settings))
        {
            Log.Warning("Settings file is empty at {SettingsFilePath}", _settingsFilePath);

            // Attempt recovery from .tmp file
            if (File.Exists(tmpPath))
            {
                Log.Warning("Attempting recovery from .tmp file at {TmpPath}", tmpPath);
                try
                {
                    settings = File.ReadAllText(tmpPath);
                    if (!string.IsNullOrWhiteSpace(settings))
                    {
                        File.Move(tmpPath, _settingsFilePath, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to recover settings from .tmp file");
                }
            }

            if (string.IsNullOrWhiteSpace(settings))
            {
                return new T();
            }
        }

        try
        {
            var decryptedSettings = Encryption.Decrypt(settings, _cryptKey, _authKey);

            if (string.IsNullOrWhiteSpace(decryptedSettings))
            {
                Log.Warning("Settings decryption returned empty result");
                return new T();
            }

            return JsonSerializer.Deserialize<T>(decryptedSettings) ?? new T();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to decrypt or deserialize settings file at {SettingsFilePath}", _settingsFilePath);
            return new T();
        }
    }
}
