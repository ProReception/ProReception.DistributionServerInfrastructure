namespace ProReception.DistributionServerInfrastructure.HostedServices;

using System.Runtime.Versioning;
using Authentication;
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
    ISettingsManagerBase settingsManagerBase,
    IAuthenticationService authenticationService)
    : IHostedService
{
    private readonly CancellationTokenSource stoppingCts = new();
    private readonly SemaphoreSlim reconnectLock = new(1, 1);

    private Task? startUpTask;
    private HubConnection? hubConnection;
    private bool isShuttingDown;

    protected abstract string HubPath { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Subscribe to logout event to restart connection with new credentials
        authenticationService.LoggedOut += OnLoggedOutAsync;

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

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"Stopping {typeof(T).Name}...");

        // Set flag to prevent reconnection attempts during shutdown
        isShuttingDown = true;

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

    private async Task OnLoggedOutAsync()
    {
        logger.LogInformation("{ServiceName}: Logout detected, disposing connection...", typeof(T).Name);

        // Dispose current connection - the Closed event handler will handle the restart
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
            hubConnection = null;
        }
    }

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
        var hasLoggedMissingTokens = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (settingsManagerBase.GetTokens() != null)
            {
                break;
            }

            if (!hasLoggedMissingTokens)
            {
                logger.LogInformation("No tokens available. Waiting for user to log in...");
                hasLoggedMissingTokens = true;
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

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

                        // During shutdown, return existing token as-is — don't attempt a refresh
                        if (stoppingCts.IsCancellationRequested)
                        {
                            logger.LogInformation("SignalR AccessTokenProvider: shutdown in progress, returning current token without refresh");
                            return current.AccessToken;
                        }

                        // Refresh token if expiring soon
                        if (current.ExpiresAtUtc.AddMinutes(-10) < DateTime.UtcNow)
                        {
                            try
                            {
                                current = await proReceptionApiClient.RefreshAndSaveTokens(current, stoppingCts.Token);
                                logger.LogInformation("SignalR AccessTokenProvider: token refreshed");
                            }
                            catch (OperationCanceledException)
                            {
                                logger.LogInformation("SignalR AccessTokenProvider: refresh cancelled during shutdown, returning current token");
                                return current.AccessToken;
                            }
                            catch (FlurlHttpException ex) when (ex.StatusCode == 401)
                            {
                                logger.LogError(ex, "SignalR AccessTokenProvider: Token refresh failed with 401 Unauthorized. Tokens have been cleared. Returning null to stop authentication attempts.");
                                // Return null to signal authentication failure
                                // This will cause SignalR connection to fail and trigger the Closed event
                                // The Closed event handler will restart ExecuteStartUp which waits for new tokens
                                return null;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "SignalR AccessTokenProvider: Failed to refresh token due to network/server error, attempting with existing token");
                                // For non-401 errors (network issues, server errors, etc.), try with existing token
                                // It might work, or fail and trigger reconnect
                                if (string.IsNullOrWhiteSpace(current.AccessToken))
                                {
                                    logger.LogError("SignalR AccessTokenProvider: No valid token available, returning null");
                                    return null;
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

            hubConnection.Closed += async error =>
            {
                if (isShuttingDown)
                {
                    logger.LogInformation("SignalR connection closed during shutdown, not reconnecting");
                    return;
                }

                logger.LogWarning(error, "SignalR connection closed unexpectedly. Attempting to reconnect...");

                // Use semaphore to prevent multiple simultaneous reconnection attempts
                if (await reconnectLock.WaitAsync(0, cancellationToken))
                {
                    try
                    {
                        // Dispose the old connection
                        if (hubConnection != null)
                        {
                            await hubConnection.DisposeAsync();
                            hubConnection = null;
                        }

                        // Restart the connection with retry logic
                        await ExecuteStartUp(stoppingCts.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to reconnect SignalR after connection closed. Will be retried by Polly policy");
                    }
                    finally
                    {
                        reconnectLock.Release();
                    }
                }
                else
                {
                    logger.LogInformation("Reconnection already in progress, skipping duplicate attempt");
                }
            };

            await hubConnection.StartAsync(cancellationToken);

            logger.LogInformation("SignalR connection successfully established");
        }
    }
}
