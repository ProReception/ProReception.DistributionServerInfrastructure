namespace ProReception.DistributionServerInfrastructure.Settings;

using Models.Public;

public interface ISettingsManagerBase
{
    TokensRecord? GetTokens();
    Task SaveTokens(string accessToken, string refreshToken, DateTime expiresAtUtc);
    Task RemoveTokens();
    string GetLogFilesPath();
}
