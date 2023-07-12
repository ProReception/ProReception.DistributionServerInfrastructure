namespace ProReception.DistributionServerInfrastructure.ProReceptionApi.Auth.Models;

using System.Text.Json.Serialization;
using JetBrains.Annotations;

[UsedImplicitly]
public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = default!;
}
