namespace ProReception.DistributionServerInfrastructure.Authentication;

using JetBrains.Annotations;

/// <summary>
/// Service for managing authentication state and notifying subscribers of changes.
/// </summary>
[PublicAPI]
public interface IAuthenticationService
{
    /// <summary>
    /// Event raised when user logs out. Subscribers should clean up site-specific state.
    /// </summary>
    event Func<Task>? LoggedOut;

    /// <summary>
    /// Logs out the user by removing tokens and raising the LoggedOut event.
    /// </summary>
    Task LogoutAsync();
}
