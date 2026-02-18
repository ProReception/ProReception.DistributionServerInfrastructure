namespace ProReception.DistributionServerInfrastructure.HostedServices;

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed class ServiceRecoveryHostedService(string serviceName, ILogger<ServiceRecoveryHostedService> logger) : IHostedService
{
    private const int ResetPeriodInSeconds = 86400; // 1 day
    private const int FirstFailureDelayMs = 5000;
    private const int SecondFailureDelayMs = 10000;
    private const int SubsequentFailureDelayMs = 30000;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            var actions = $"restart/{FirstFailureDelayMs}/restart/{SecondFailureDelayMs}/restart/{SubsequentFailureDelayMs}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"failure \"{serviceName}\" reset= {ResetPeriodInSeconds} actions= {actions}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                logger.LogInformation("Service recovery settings configured for '{ServiceName}'", serviceName);
            }
            else
            {
                logger.LogWarning(
                    "Could not configure service recovery settings for '{ServiceName}'. Exit code: {ExitCode}. {Error}",
                    serviceName, process.ExitCode, string.IsNullOrWhiteSpace(error) ? output : error);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not configure service recovery settings for '{ServiceName}'", serviceName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
