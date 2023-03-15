namespace ProReception.DistributionServerInfrastructure.Settings.Models.Internal;

internal class Tokens
{
    public string AccessToken { get; set; } = default!;

    public string RefreshToken { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; } = default!;
}
