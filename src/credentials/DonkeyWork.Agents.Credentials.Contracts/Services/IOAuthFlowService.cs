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
    /// Stores the state, code verifier, and user context in the database.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="provider">OAuth provider.</param>
    /// <param name="scopes">Optional scopes to request. If null, uses the scopes from the provider configuration or provider defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization URL and state parameter.</returns>
    Task<(string AuthorizationUrl, string State)> GenerateAuthorizationUrlAsync(
        Guid userId,
        OAuthProvider provider,
        IReadOnlyList<string>? scopes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the OAuth state parameter and returns the stored callback context.
    /// Consumes (deletes) the state so it cannot be reused.
    /// </summary>
    /// <param name="state">The state parameter from the OAuth callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The callback context if valid, null if state is invalid or expired.</returns>
    Task<OAuthCallbackState?> ValidateAndConsumeStateAsync(
        string state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles the OAuth callback, exchanges code for tokens, and stores them.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="provider">OAuth provider.</param>
    /// <param name="code">Authorization code from callback.</param>
    /// <param name="codeVerifier">PKCE code verifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored OAuth token.</returns>
    Task<OAuthToken> HandleCallbackAsync(
        Guid userId,
        OAuthProvider provider,
        string code,
        string codeVerifier,
        CancellationToken cancellationToken = default);
}
