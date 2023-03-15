namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProReception.DistributionServerInfrastructure.Configuration;
using ProReception.DistributionServerInfrastructure.Settings;

public class ProReceptionApiClient : ApiClientBase, IProReceptionApiClient
{
    public ProReceptionApiClient(ILogger<ProReceptionApiClient> logger, ISettingsManagerBase settingsManagerBase, IOptions<ProReceptionApiConfiguration> options)
        : base(logger, settingsManagerBase, options)
    {
    }

    public async Task<T> Get<T>(string path) => await Query(req => req.AppendPathSegment(path).GetJsonAsync<T>());

    public async Task Post<T>(string path, T data) => await Command(req => req.AppendPathSegment(path).PostJsonAsync(data));

    public async Task Put<T>(string path, T data) => await Command(req => req.AppendPathSegment(path).PutJsonAsync(data));

    public async Task Patch<T>(string path, T data) => await Command(req => req.AppendPathSegment(path).PatchJsonAsync(data));
}
