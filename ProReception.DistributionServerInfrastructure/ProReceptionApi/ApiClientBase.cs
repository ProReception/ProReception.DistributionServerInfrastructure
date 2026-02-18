namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Auth.Models;
using Configuration;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;
using Settings;
using Settings.Models.Public;

public abstract class ApiClientBase
{
    private readonly ILogger<ApiClientBase> _logger;
    private readonly ISettingsManagerBase _settingsManagerBase;
    private readonly ProReceptionApiConfiguration _configuration;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected ApiClientBase(
        ILogger<ApiClientBase> logger,
        ISettingsManagerBase settingsManagerBase,
        IOptions<ProReceptionApiConfiguration> options)
    {
        _logger = logger;
        _settingsManagerBase = settingsManagerBase;
        _configuration = options.Value;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRateLimiter(new RateLimiterStrategyOptions())
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMinutes(1) })
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = ShouldHandle(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    var nextAttemptIn = args.RetryDelay;
                    logger.LogWarning("Request failed. Making retry attempt '{RetryAttempt}' in {NextAttemptInSeconds} seconds.", args.AttemptNumber, nextAttemptIn.TotalSeconds);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ShouldHandle = ShouldHandle() })
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(30) })
            .Build();
    }

    public async Task<TokensRecord> GetAndSaveTokens(string username, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging in to ProReception...");

        var response = await _configuration.BaseUrl
            .AppendPathSegment("auth/login")
            .PostJsonAsync(new { username, password }, cancellationToken: cancellationToken)
            .ReceiveJson<TokenResponse>();

        return await SaveTokensToSettings(response);
    }

    public async Task<TokensRecord> RefreshAndSaveTokens(TokensRecord tokensRecord, CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-read current tokens — another caller may have already refreshed while we waited
            var current = _settingsManagerBase.GetTokens();
            if (current != null && DateTime.UtcNow < current.ExpiresAtUtc.AddMinutes(-1))
            {
                _logger.LogInformation("Tokens were already refreshed by another caller, skipping refresh");
                return current;
            }

            _logger.LogInformation("Refreshing ProReception tokens...");

            try
            {
                var response = await _configuration.BaseUrl
                    .AppendPathSegment("auth/refresh")
                    .PostJsonAsync(new { tokensRecord.AccessToken, tokensRecord.RefreshToken }, cancellationToken: cancellationToken)
                    .ReceiveJson<TokenResponse>();

                return await SaveTokensToSettings(response);
            }
            catch (FlurlHttpException ex) when (ex.StatusCode == 401)
            {
                _logger.LogWarning("Token refresh failed with 401 Unauthorized. Clearing stored tokens.");
                await _settingsManagerBase.RemoveTokens();
                throw; // Re-throw so caller knows refresh failed
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<T> Query<T>(Func<IFlurlRequest, Task<T>> getRequestFunc) =>
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var baseRequest = await GetBaseRequestAsync();

            return await getRequestFunc(baseRequest);
        });

    protected async Task Command(Func<IFlurlRequest, Task> postRequestFunc) =>
        await _resiliencePipeline.ExecuteAsync(async _ =>
        {
            var baseRequest = await GetBaseRequestAsync();

            await postRequestFunc(baseRequest);
        });

    private async Task<TokensRecord> SaveTokensToSettings(TokenResponse response)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        await _settingsManagerBase.SaveTokens(response.AccessToken, response.RefreshToken, jwtToken.ValidTo);

        return _settingsManagerBase.GetTokens()!;
    }

    private static PredicateBuilder<object> ShouldHandle() =>
        new PredicateBuilder().Handle<FlurlHttpException>(ex =>
            ex.Call.Response?.StatusCode >= 500 ||
            ex.Call.Response?.StatusCode == (int)HttpStatusCode.RequestTimeout ||
            ex.Call.Response?.StatusCode == 429);

    private async Task<IFlurlRequest> GetBaseRequestAsync()
    {
        return _configuration.BaseUrl
            .WithOAuthBearerToken(await GetAccessTokenAsync());
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokensRecord = _settingsManagerBase.GetTokens();

        if (tokensRecord == null)
        {
            throw new InvalidOperationException("You have to log in to Pro Reception first");
        }

        if (DateTime.UtcNow < tokensRecord.ExpiresAtUtc.AddMinutes(-1))
        {
            return tokensRecord.AccessToken;
        }

        try
        {
            var refreshedTokens = await RefreshAndSaveTokens(tokensRecord);
            return refreshedTokens.AccessToken;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 401)
        {
            // Tokens were already cleared in RefreshAndSaveTokens
            throw new InvalidOperationException("Your session has expired. Please log in again.", ex);
        }
    }
}
