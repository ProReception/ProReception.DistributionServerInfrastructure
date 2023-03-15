namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.Models;

using System.Text.Json.Serialization;

public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = default!;
}
