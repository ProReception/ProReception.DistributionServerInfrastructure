namespace ProReception.DistributionServerInfrastructure;

using Hubs;
using Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    public static void UseProReceptionDistributionServerInfrastructure(this WebApplication app)
    {
        app.MapHub<LogsHub>("/logs-hub");
    }
}
