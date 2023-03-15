namespace ProReception.DistributionServerInfrastructure.Settings.Models.Public;

public record TokensRecord(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
