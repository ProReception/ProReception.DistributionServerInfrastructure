namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

public class SaveNoxAccessRingsRequest
{
    public int ClientSiteId { get; set; }

    public List<NoxAccessRing> AccessRings { get; set; } = new();

    public class NoxAccessRing
    {
        public required string Name { get; set; }

        public int NoxAccessRingId { get; set; }
    }
}
