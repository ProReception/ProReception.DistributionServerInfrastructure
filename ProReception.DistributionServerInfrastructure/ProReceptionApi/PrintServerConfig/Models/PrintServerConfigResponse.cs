namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.PrintServerConfig.Models;

public class PrintServerConfigResponse
{
    public bool PrintEnabled { get; set; }

    public required string ClientName { get; set; }

    public required string ClientSiteName { get; set; }
}
