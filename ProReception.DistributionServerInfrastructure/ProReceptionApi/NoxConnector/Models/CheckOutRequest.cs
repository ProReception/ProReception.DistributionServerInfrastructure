namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

public class CheckOutRequest
{
    public required string Location { get; set; }

    public DateTime Timestamp { get; set; }
}
