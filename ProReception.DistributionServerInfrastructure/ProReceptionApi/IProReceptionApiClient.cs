namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using ProReception.DistributionServerInfrastructure.Settings.Models.Public;

public interface IProReceptionApiClient
{
    Task<TokensRecord> GetAndSaveTokens(string username, string password);
    Task<TokensRecord> RefreshAndSaveTokens(TokensRecord tokensRecord);
    Task<T> Get<T>(string path);
    Task Post<T>(string path, T data);
    Task Put<T>(string path, T data);
    Task Patch<T>(string path, T data);
}
