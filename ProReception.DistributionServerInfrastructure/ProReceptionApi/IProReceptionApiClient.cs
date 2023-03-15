namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using Flurl.Http;
using ProReception.DistributionServerInfrastructure.Settings.Models.Public;

public interface IProReceptionApiClient
{
    Task<TokensRecord> GetAndSaveTokens(string username, string password);
    Task<TokensRecord> RefreshAndSaveTokens(TokensRecord tokensRecord);
    Task<T> Get<T>(string path);
    Task Post(string path, object data);
    Task Put(string path, object data);
    Task Patch(string path, object data);
    Task<IFlurlResponse> GetRaw(string path);
}
