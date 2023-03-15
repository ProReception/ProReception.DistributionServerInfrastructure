namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

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
}
