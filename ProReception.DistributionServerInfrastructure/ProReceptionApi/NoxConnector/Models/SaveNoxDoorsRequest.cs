namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

using JetBrains.Annotations;

[PublicAPI]
public class SaveNoxDoorsRequest
{
    public int ClientSiteId { get; set; }

    public List<NoxDoor> NoxDoors { get; set; } = new();

    public class NoxDoor
    {
        public required string Name { get; set; }

        public int NoxAreaId { get; set; }
    }
}
