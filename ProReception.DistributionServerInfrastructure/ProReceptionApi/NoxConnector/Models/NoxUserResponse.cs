namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

using JetBrains.Annotations;

[PublicAPI]
public class NoxUserResponse
{
    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public string? CompanyName { get; set; }

    public int? BadgeId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public string? PhotoUrl { get; set; }

    public bool IsEnabled { get; set; }

    public int? NoxPinCode { get; set; }

    public int? AlarmCode { get; set; }

    public List<NoxArea> NoxAreas { get; set; } = new();

    public string? NoxUsername { get; set; }

    public string? CardNumber { get; set; }

    public int? CardExchangeIdentifier { get; set; }

    public int? NoxUserGroupNumber { get; set; }

    public class NoxArea
    {
        public short NoxSystemNumber { get; set; }
        public int NoxAreaNumber { get; set; }
    }
}
