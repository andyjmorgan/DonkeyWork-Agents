using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

/// <summary>
/// Factory for creating OAuth provider instances.
/// </summary>
public interface IOAuthProviderFactory
{
    /// <summary>
    /// Gets an OAuth provider instance for the specified provider type.
    /// For built-in providers (Google, Microsoft, GitHub), config is not required.
    /// For Custom providers, config must include endpoint URLs.
    /// </summary>
    /// <param name="provider">The OAuth provider type.</param>
    /// <param name="config">Optional provider config with custom URLs (required for Custom providers).</param>
    /// <returns>An OAuth provider implementation.</returns>
    IOAuthProvider GetProvider(OAuthProvider provider, OAuthProviderConfig? config = null);
}
