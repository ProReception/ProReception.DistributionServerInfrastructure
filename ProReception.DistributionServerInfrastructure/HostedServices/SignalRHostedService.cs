namespace ProReception.DistributionServerInfrastructure.HostedServices;

using Configuration;
using Flurl;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProReceptionApi;
using Settings;
using Settings.Models.Public;

public abstract class SignalRHostedService<T> : IHostedService
{
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly ILogger<T> _logger;
    private readonly IProReceptionApiClient _proReceptionApiClient;
    private readonly ISettingsManagerBase _settingsManagerBase;
    private readonly ProReceptionApiConfiguration _proReceptionApiConfiguration;

    private Task? _startUpTask;
    private HubConnection? _hubConnection;

    protected SignalRHostedService(
        IOptions<ProReceptionApiConfiguration> options,
        ILogger<T> logger,
        IProReceptionApiClient proReceptionApiClient,
        ISettingsManagerBase settingsManagerBase)
    {
        _logger = logger;
        _proReceptionApiClient = proReceptionApiClient;
        _settingsManagerBase = settingsManagerBase;
        _proReceptionApiConfiguration = options.Value;
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

    private async Task ExecuteStartUp(CancellationToken cancellationToken, int tryCount = 1)
    {
        _logger.LogInformation($"Starting {typeof(T).Name}...");

        var proReceptionTokens = await GetProReceptionTokens(cancellationToken);

        while(!cancellationToken.IsCancellationRequested && proReceptionTokens == null)
        {
            proReceptionTokens = await GetProReceptionTokens(cancellationToken);
        }

        if (!cancellationToken.IsCancellationRequested && !string.IsNullOrWhiteSpace(proReceptionTokens?.AccessToken))
        {
            _logger.LogInformation("Establishing SignalR connection...");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_proReceptionApiConfiguration.BaseUrl.AppendPathSegment(HubPath), options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {proReceptionTokens.AccessToken}");
                })
                .WithAutomaticReconnect()
                .Build();

            ConfigureListeners(_hubConnection);

            _hubConnection.Closed += async _ =>
            {
                var secondsDelay = 10 * tryCount;
                _logger.LogInformation("SignalR connection lost, will retry in {secondsDelay} seconds", secondsDelay);

                await _hubConnection.StopAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(secondsDelay), cancellationToken);
                await ExecuteStartUp(cancellationToken, ++tryCount);
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
                if (proReceptionTokens.ExpiresAtUtc < DateTime.UtcNow.AddMinutes(-10))
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
