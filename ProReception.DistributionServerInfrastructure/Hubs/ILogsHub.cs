namespace ProReception.DistributionServerInfrastructure.Hubs;

public interface ILogsHub
{
    Task ReceiveLog(LogsHub.LogMessage logMessage);
}
