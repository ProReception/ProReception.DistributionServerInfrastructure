namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.ActConnector.Models;

public class SaveActUserGroupsRequest
{
    public List<ActUserGroup> ActUserGroups { get; set; } = new List<ActUserGroup>();

    public class ActUserGroup
    {
        public int UserGroupNumber { get; set; }
        public required string Name { get; set; }
    }
}
