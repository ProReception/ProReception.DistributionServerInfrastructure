namespace ProReception.DistributionServerInfrastructure.FlurlClientFactories;

using System.Net;
using System.Runtime.Versioning;
using Configuration;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;

[UnsupportedOSPlatform("browser")] // Proxy support in HttpClientHandler is not supported in browser
public class ProxyFlurlClientFactory : DefaultHttpClientFactory
{
    private readonly IWebProxy? _proxy;

    public ProxyFlurlClientFactory(IWebProxy? proxy)
    {
        _proxy = proxy;
    }

    public override HttpMessageHandler CreateMessageHandler()
    {
        var httpMessageHandler = base.CreateMessageHandler();

        if (_proxy == null)
        {
            return httpMessageHandler;
        }

        var httpClientHandler = (HttpClientHandler)httpMessageHandler;

        httpClientHandler.Proxy = _proxy;
        httpClientHandler.UseProxy = true;

        return httpClientHandler;
    }
}
