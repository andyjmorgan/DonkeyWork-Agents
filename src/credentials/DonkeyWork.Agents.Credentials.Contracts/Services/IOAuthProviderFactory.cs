using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

/// <summary>
/// Factory for creating OAuth provider instances.
/// </summary>
public interface IOAuthProviderFactory
{
    /// <summary>
    /// Gets an OAuth provider instance for the specified provider type.
    /// </summary>
    /// <param name="provider">The OAuth provider type.</param>
    /// <returns>An OAuth provider implementation.</returns>
    IOAuthProvider GetProvider(OAuthProvider provider);
}
