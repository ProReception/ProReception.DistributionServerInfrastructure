namespace ProReception.DistributionServerInfrastructure.ProReceptionApi;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Configuration;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Polly;
using Polly.Retry;
using Settings;
using Settings.Models.Public;

public abstract class ApiClientBase
{
    private readonly ILogger<ApiClientBase> _logger;
    private readonly ISettingsManagerBase _settingsManagerBase;
    private readonly ProReceptionApiConfiguration _configuration;
    private readonly AsyncRetryPolicy _retryPolicy;

    protected ApiClientBase(
        ILogger<ApiClientBase> logger,
        ISettingsManagerBase settingsManagerBase,
        IOptions<ProReceptionApiConfiguration> options)
    {
        _logger = logger;
        _settingsManagerBase = settingsManagerBase;
        _configuration = options.Value;

        _retryPolicy = Policy
            .Handle<FlurlHttpException>(IsWorthRetrying)
            .WaitAndRetryAsync(4, retryAttempt =>
            {
                var nextAttemptIn = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt * 3)); // Wait times: 9s, 36s, 81s & 144s ~ 4,5m in total
                logger.LogInformation("Retry attempt {retryAttempt}. Next try in {nextAttemptInSeconds} seconds.", retryAttempt, nextAttemptIn.TotalSeconds);
                return nextAttemptIn;
            });
    }

    public async Task<TokensRecord> GetAndSaveTokens(string username, string password)
    {
        _logger.LogInformation("Logging in to ProReception...");

        var response = await _configuration.BaseUrl
            .AppendPathSegment("auth/login")
            .PostJsonAsync(new { username, password })
            .ReceiveJson<TokenResponse>();

        return await SaveTokensToSettings(response);
    }

    public async Task<TokensRecord> RefreshAndSaveTokens(TokensRecord tokensRecord)
    {
        _logger.LogInformation("Refreshing ProReception tokens...");

        var response = await _configuration.BaseUrl
            .AppendPathSegment("auth/refresh")
            .PostJsonAsync(new { tokensRecord.AccessToken, tokensRecord.RefreshToken })
            .ReceiveJson<TokenResponse>();

        return await SaveTokensToSettings(response);
    }

    protected async Task<T> Query<T>(Func<IFlurlRequest, Task<T>> getRequestFunc)
    {
        var baseRequest = await GetBaseRequestAsync();

        return await _retryPolicy.ExecuteAsync(() => getRequestFunc(baseRequest));
    }

    protected async Task Command(Func<IFlurlRequest, Task> postRequestFunc)
    {
        var baseRequest = await GetBaseRequestAsync();

        await _retryPolicy.ExecuteAsync(() => postRequestFunc(baseRequest));
    }

    private async Task<TokensRecord> SaveTokensToSettings(TokenResponse response)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        await _settingsManagerBase.SaveTokens(response.AccessToken, response.RefreshToken, jwtToken.ValidTo);

        return _settingsManagerBase.GetTokens()!;
    }

    private static bool IsWorthRetrying(FlurlHttpException ex) {
        switch (ex.Call.Response.StatusCode) {
            case (int)HttpStatusCode.RequestTimeout: // 408
            case (int)HttpStatusCode.BadGateway: // 502
            case (int)HttpStatusCode.ServiceUnavailable: // 503
            case (int)HttpStatusCode.GatewayTimeout: // 504
                return true;
            default:
                return false;
        }
    }

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

        var refreshedTokens = await RefreshAndSaveTokens(tokensRecord);

        return refreshedTokens.AccessToken;
    }
}
