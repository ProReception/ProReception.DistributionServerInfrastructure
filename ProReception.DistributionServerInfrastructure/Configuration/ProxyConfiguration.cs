namespace ProReception.DistributionServerInfrastructure.Configuration;

using System.Net;

public class ProxyConfiguration
{
    public string? Address { get; set; }

    public IWebProxy? GetWebProxy() => string.IsNullOrWhiteSpace(Address) ? null : new WebProxy(Address);
}
