namespace ProReception.DistributionServerInfrastructure.Settings.Models;

using Internal;

public abstract class BaseSettings
{
    public Guid DistributionServerAppId { get; set; } = Guid.NewGuid();
    public Tokens? ProReceptionTokens { get; set; }
}
