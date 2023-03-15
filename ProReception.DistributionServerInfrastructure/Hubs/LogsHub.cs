namespace ProReception.DistributionServerInfrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

public class LogsHub : Hub<ILogsHub>
{
    public class LogMessage
    {
        public DateTimeOffset Timestamp { get; set; }

        public string Message { get; set; } = default!;
    }
}
