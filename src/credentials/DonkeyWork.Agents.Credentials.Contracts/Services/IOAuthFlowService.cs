using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

/// <summary>
/// Service for orchestrating OAuth authorization flows.
/// </summary>
public interface IOAuthFlowService
{
    /// <summary>
    /// Generates an authorization URL for the OAuth flow with PKCE.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="provider">OAuth provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization URL, state parameter, and code verifier.</returns>
    Task<(string AuthorizationUrl, string State, string CodeVerifier)> GenerateAuthorizationUrlAsync(
        Guid userId,
        OAuthProvider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles the OAuth callback, exchanges code for tokens, and stores them.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="provider">OAuth provider.</param>
    /// <param name="code">Authorization code from callback.</param>
    /// <param name="state">State parameter for CSRF validation.</param>
    /// <param name="codeVerifier">PKCE code verifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored OAuth token.</returns>
    Task<OAuthToken> HandleCallbackAsync(
        Guid userId,
        OAuthProvider provider,
        string code,
        string state,
        string codeVerifier,
        CancellationToken cancellationToken = default);
}
