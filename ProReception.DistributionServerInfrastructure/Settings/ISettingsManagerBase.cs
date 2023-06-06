namespace ProReception.DistributionServerInfrastructure.Settings;

using Models.Public;

public interface ISettingsManagerBase
{
    Guid GetDistributionServerAppId();
    TokensRecord? GetTokens();
    Task SaveTokens(string accessToken, string refreshToken, DateTime expiresAtUtc);
    Task RemoveTokens();
    string GetLogFilesPath();
}
