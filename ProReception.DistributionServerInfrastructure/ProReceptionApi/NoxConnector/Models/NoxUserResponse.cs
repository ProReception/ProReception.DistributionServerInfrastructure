namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.NoxConnector.Models;

using JetBrains.Annotations;

[UsedImplicitly]
public class NoxUserResponse
{
    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public required string BadgeId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public string? PhotoUrl { get; set; }

    public bool IsEnabled { get; set; }

    public string? CompanyName { get; set; }

    public string? TradeName { get; set; }

    public DateTime? InductionDate { get; set; }

    public int? ActUserGroupNumber { get; set; }
}
