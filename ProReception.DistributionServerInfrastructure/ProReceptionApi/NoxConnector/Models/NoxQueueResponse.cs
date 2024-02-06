namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

public class NoxQueueResponse
{
    public required string Name { get; set; }

    public DateTime SasUriExpiryUtc { get; set; }

    public required string SasUri { get; set; }
}
