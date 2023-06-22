namespace ProReception.DistributionServerInfrastructure.FlurlClientFactories;

using System.Net;
using Configuration;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;

public class ProxyFlurlClientFactory : DefaultHttpClientFactory
{
    private readonly IWebProxy? _proxy;

    public ProxyFlurlClientFactory(IConfigurationSection proxyConfigurationSection)
    {
        if (proxyConfigurationSection.Exists())
        {
            _proxy = proxyConfigurationSection.Get<ProxyConfiguration>()?.GetWebProxy();
        }
    }

    public override HttpMessageHandler CreateMessageHandler()
    {
        var httpMessageHandler = base.CreateMessageHandler();

        if (_proxy == null)
        {
            return httpMessageHandler;
        }

        var httpClientHandler = (HttpClientHandler)httpMessageHandler;

#pragma warning disable CA1416
        httpClientHandler.Proxy = _proxy;
        httpClientHandler.UseProxy = true;
#pragma warning restore CA1416

        return httpClientHandler;
    }
}
