namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.ActConnector.Models;

using JetBrains.Annotations;

[UsedImplicitly]
public class ActQueueResponse
{
    public required string Name { get; set; }

    public DateTime SasUriExpiryUtc { get; set; }

    public required string SasUri { get; set; }
}
