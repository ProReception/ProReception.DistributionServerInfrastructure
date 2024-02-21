namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

using JetBrains.Annotations;

[PublicAPI]
public class NoxUserResponse
{
    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public int? BadgeId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public string? PhotoUrl { get; set; }

    public bool IsEnabled { get; set; }

    public int? NoxPinCode { get; set; }

    public List<int> NoxAreaNumbers { get; set; } = new();

    public string? NoxUsername { get; set; }
}
