namespace ProReception.DistributionServerInfrastructure.Authentication;

using Microsoft.Extensions.Logging;
using Settings;

public class AuthenticationService(
    ISettingsManagerBase settingsManagerBase,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public event Func<Task>? LoggedOut;

    public async Task LogoutAsync()
    {
        logger.LogInformation("Logging out user...");

        // Clear site-specific settings (implemented by app's SettingsManager)
        await settingsManagerBase.OnUserLoggedOutAsync();

        // Remove tokens
        await settingsManagerBase.RemoveTokens();

        // Notify subscribers (e.g., SignalRHostedService)
        if (LoggedOut != null)
        {
            logger.LogInformation("Notifying {Count} subscriber(s) of logout", LoggedOut.GetInvocationList().Length);
            await LoggedOut.Invoke();
        }

        logger.LogInformation("Logout complete");
    }
}
