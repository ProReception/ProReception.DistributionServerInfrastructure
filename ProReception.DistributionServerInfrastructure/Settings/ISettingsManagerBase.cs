namespace ProReception.DistributionServerInfrastructure.Settings;

using Models.Public;

public interface ISettingsManagerBase
{
    Guid GetDistributionServerAppId();
    TokensRecord? GetTokens();
    Task SaveTokens(string accessToken, string refreshToken, DateTime expiresAtUtc);
    Task RemoveTokens();
    string GetLogFilesPath();

    /// <summary>
    /// Called when the user logs out. Override to clear site-specific settings.
    /// </summary>
    Task OnUserLoggedOutAsync();
}
