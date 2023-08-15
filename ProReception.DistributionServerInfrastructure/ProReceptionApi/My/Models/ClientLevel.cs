namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.My.Models;

public class ClientLevel
{
    public int ClientLevelId { get; set; }

    public short ClientLevelHierarchyLevel { get; set; }

    public required string Name { get; set; }

    public required string Hierarchy { get; set; }
}
