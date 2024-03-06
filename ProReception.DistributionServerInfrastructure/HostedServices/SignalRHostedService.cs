namespace ProReception.DistributionServerInfrastructure.HostedServices;

using System.Runtime.Versioning;
using Configuration;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ProReceptionApi;
using Settings;
using Settings.Models.Public;

[UnsupportedOSPlatform("browser")] // Proxy support in SignalR is not supported in browser
public abstract class SignalRHostedService<T> : IHostedService
{
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly ILogger<T> _logger;
    private readonly IProReceptionApiClient _proReceptionApiClient;
    private readonly ISettingsManagerBase _settingsManagerBase;
    private readonly ProReceptionApiConfiguration _proReceptionApiConfiguration;
    private readonly ProxyConfiguration _proxyConfiguration;

    private Task? _startUpTask;
    private HubConnection? _hubConnection;

    protected SignalRHostedService(
        IOptions<ProReceptionApiConfiguration> proReceptionApiConfigurationOptions,
        IOptions<ProxyConfiguration> proxyConfigurationOptions,
        ILogger<T> logger,
        IProReceptionApiClient proReceptionApiClient,
        ISettingsManagerBase settingsManagerBase)
    {
        _logger = logger;
        _proReceptionApiClient = proReceptionApiClient;
        _settingsManagerBase = settingsManagerBase;
        _proReceptionApiConfiguration = proReceptionApiConfigurationOptions.Value;
        _proxyConfiguration = proxyConfigurationOptions.Value;
    }

    protected abstract string HubPath { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startUpTask = ExecuteStartUp(_stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Stopping {typeof(T).Name}...");

        // Stop called without start
        if (_startUpTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            _stoppingCts.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_startUpTask, Task.Delay(Timeout.Infinite, cancellationToken));

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }

    protected abstract void ConfigureListeners(HubConnection hubConnection);

    private async Task ExecuteStartUp(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {typeof(T).Name}...");

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

                    _logger.LogWarning("Attempt {AttemptNumber} failed: {ExceptionMessage}. Waiting {RetryDelay} before next try.", args.AttemptNumber, exceptionMessage, args.RetryDelay);
                }
            })
            .Build()
            .ExecuteAsync(async token => await LoginAndCreateSignalRConnection(token), cancellationToken);
    }

    private async Task LoginAndCreateSignalRConnection(CancellationToken cancellationToken)
    {
        TokensRecord? proReceptionTokens;
        do
        {
            proReceptionTokens = await GetProReceptionTokens(cancellationToken);
        } while (!cancellationToken.IsCancellationRequested && string.IsNullOrWhiteSpace(proReceptionTokens?.AccessToken));

        if (!cancellationToken.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(proReceptionTokens?.AccessToken))
            {
                throw new ApplicationException("The ProReception access token is null or empty (this should never happen)");
            }

            _logger.LogInformation("Establishing SignalR connection...");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_proReceptionApiConfiguration.BaseUrl.AppendPathSegment(HubPath), options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {proReceptionTokens.AccessToken}");
                    options.Headers.Add("X-DistributionServerAppId", _settingsManagerBase.GetDistributionServerAppId().ToString());
                    options.Proxy = _proxyConfiguration.GetWebProxy();
                })
                .WithAutomaticReconnect()
                .Build();

            ConfigureListeners(_hubConnection);

            _hubConnection.Closed += async _ =>
            {
                _logger.LogInformation("SignalR connection lost, will retry...");
                await _hubConnection.StopAsync(cancellationToken);
                await ExecuteStartUp(cancellationToken);
            };

            await _hubConnection.StartAsync(cancellationToken);

            _logger.LogInformation("SignalR connection successfully established");
        }
    }

    private async Task<TokensRecord?> GetProReceptionTokens(CancellationToken cancellationToken)
    {
        while(!cancellationToken.IsCancellationRequested)
        {
            var proReceptionTokens = _settingsManagerBase.GetTokens();

            if (!string.IsNullOrWhiteSpace(proReceptionTokens?.AccessToken))
            {
                if (proReceptionTokens.ExpiresAtUtc.AddMinutes(-10) < DateTime.UtcNow)
                {
                    return await _proReceptionApiClient.RefreshAndSaveTokens(proReceptionTokens);
                }

                return proReceptionTokens;
            }

            _logger.LogInformation("Not logged into ProReception, sleeping...");

            await Task.Delay(1000, cancellationToken);
        }

        return null;
    }
}
