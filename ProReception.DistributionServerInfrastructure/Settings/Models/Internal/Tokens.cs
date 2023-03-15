namespace ProReception.DistributionServerInfrastructure.Settings.Models.Internal;

public class Tokens
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
