namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using ProReception.DistributionServerInfrastructure.Settings.Models.Public;

public interface IProReceptionApiClient
{
    Task<TokensRecord> GetAndSaveTokens(string username, string password);

    Task<TokensRecord> RefreshAndSaveTokens(TokensRecord tokensRecord);
}
