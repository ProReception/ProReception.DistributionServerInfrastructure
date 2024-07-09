namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

public class SaveNoxUserGroupsRequest
{
    public int ClientSiteId { get; set; }

    public List<NoxUserGroup> UserGroups { get; set; } = new();

    public class NoxUserGroup
    {
        public required string Name { get; set; }

        public int Number { get; set; }
    }
}
