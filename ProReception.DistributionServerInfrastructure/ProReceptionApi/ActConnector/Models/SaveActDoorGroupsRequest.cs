namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.ActConnector.Models;

public class SaveActDoorGroupsRequest
{
    public List<ActDoorGroup> ActDoorGroups { get; set; } = new List<ActDoorGroup>();

    public class ActDoorGroup
    {
        public int DoorGroupNumber { get; set; }
        public required string Name { get; set; }
    }
}
