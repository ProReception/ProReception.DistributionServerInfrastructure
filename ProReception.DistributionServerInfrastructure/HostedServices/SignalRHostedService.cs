namespace ProReception.DistributionServerInfrastructure.HostedServices;

using System.Runtime.Versioning;
using Configuration;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ProReceptionApi;
using Settings;

[PublicAPI]
[UnsupportedOSPlatform("browser")] // Proxy support in SignalR is not supported in browser
public abstract class SignalRHostedService<T>(
    IOptions<ProReceptionApiConfiguration> proReceptionApiConfigurationOptions,
    IOptions<ProxyConfiguration> proxyConfigurationOptions,
    ILogger<T> logger,
    IProReceptionApiClient proReceptionApiClient,
    ISettingsManagerBase settingsManagerBase)
    : IHostedService
{
    private readonly CancellationTokenSource stoppingCts = new();

    private Task? startUpTask;
    private HubConnection? hubConnection;

    protected abstract string HubPath { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        startUpTask = ExecuteStartUp(stoppingCts.Token);

        // Observe exceptions from the background task to prevent unobserved task exceptions from crashing the service
        _ = startUpTask.ContinueWith(task =>
        {
            if (task.IsFaulted && task.Exception != null)
            {
                logger.LogError(task.Exception, "Fatal error in {ServiceName} startup - service may be in degraded state", typeof(T).Name);
            }
        }, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"Stopping {typeof(T).Name}...");

        // Stop called without start
        if (startUpTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            await stoppingCts.CancelAsync();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(startUpTask, Task.Delay(Timeout.Infinite, cancellationToken));

            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
            }
        }
    }

    protected abstract void ConfigureListeners(HubConnection hubConnection);

    private async Task ExecuteStartUp(CancellationToken cancellationToken)
    {
        logger.LogInformation($"Starting {typeof(T).Name}...");

        await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = Int32.MaxValue,
                Delay = TimeSpan.FromSeconds(3),
                UseJitter = true,
                BackoffType = DelayBackoffType.Constant,
                OnRetry = async args =>
                {
                    var exceptionMessage = args.Outcome.Exception?.Message ?? string.Empty;
                    if (args.Outcome.Exception is FlurlHttpException flurlException)
                    {
                        exceptionMessage += $" Response body: {await flurlException.GetResponseStringAsync()}";
                    }

                    logger.LogWarning("Attempt {AttemptNumber} failed: {ExceptionMessage}. Waiting {RetryDelay} before next try", args.AttemptNumber, exceptionMessage, args.RetryDelay);
                }
            })
            .Build()
            .ExecuteAsync(async token => await LoginAndCreateSignalRConnection(token), cancellationToken);
    }

    private async Task LoginAndCreateSignalRConnection(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Establishing SignalR connection...");

            hubConnection = new HubConnectionBuilder()
                .WithUrl(proReceptionApiConfigurationOptions.Value.BaseUrl.AppendPathSegment(HubPath), options =>
                {
                    // Provide dynamic access token for every (re)connect to avoid stale tokens
                    options.AccessTokenProvider = async () =>
                    {
                        var current = settingsManagerBase.GetTokens();
                        if (current is null)
                        {
                            // No tokens available (e.g., user logged out) -> return null to let reconnect keep retrying
                            logger.LogInformation("SignalR AccessTokenProvider: no tokens available");
                            return null;
                        }

                        // Refresh token if expiring soon
                        if (current.ExpiresAtUtc.AddMinutes(-10) < DateTime.UtcNow)
                        {
                            try
                            {
                                current = await proReceptionApiClient.RefreshAndSaveTokens(current);
                                logger.LogInformation("SignalR AccessTokenProvider: token refreshed");
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "SignalR AccessTokenProvider: CRITICAL - failed to refresh token, using existing token");
                                // Return existing token (may fail with 401 and trigger reconnect)
                                // Don't throw - let SignalR handle the authentication failure and reconnect
                                if (string.IsNullOrWhiteSpace(current.AccessToken))
                                {
                                    logger.LogError("SignalR AccessTokenProvider: No valid token available, returning empty string");
                                    return string.Empty;
                                }
                            }
                        }

                        return current.AccessToken;
                    };

                    options.Headers.Add("X-DistributionServerAppId", settingsManagerBase.GetDistributionServerAppId().ToString());
                    options.Proxy = proxyConfigurationOptions.Value.GetWebProxy();
                })
                .WithAutomaticReconnect()
                .Build();

            ConfigureListeners(hubConnection);

            hubConnection.Reconnecting += error =>
            {
                logger.LogInformation(error, "SignalR connection reconnecting...");
                return Task.CompletedTask;
            };

            hubConnection.Reconnected += connectionId =>
            {
                logger.LogInformation("SignalR connection reconnected. ConnectionId={ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            hubConnection.Closed += error =>
            {
                // Do not call StopAsync or recursively restart; rely on automatic reconnect and outer retry/startup logic
                logger.LogInformation(error, "SignalR connection closed. Waiting for background retry logic...");
                return Task.CompletedTask;
            };

            await hubConnection.StartAsync(cancellationToken);

            logger.LogInformation("SignalR connection successfully established");
        }
    }
}
