namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

public class CheckInRequest
{
    public required string Location { get; set; }

    public DateTime Timestamp { get; set; }

    public int NoxSystemNumber { get; set; }

    public int AccessRingRingId { get; set; }
}
