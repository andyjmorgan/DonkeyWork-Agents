using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// OAuth flow endpoints for authorization and callback handling.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/oauth")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthFlowService _oauthFlowService;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IOAuthFlowService oauthFlowService,
        IIdentityContext identityContext,
        ILogger<OAuthController> logger)
    {
        _oauthFlowService = oauthFlowService;
        _identityContext = identityContext;
        _logger = logger;
    }

    /// <summary>
    /// Generates an OAuth authorization URL for the specified provider.
    /// Stores flow state server-side for secure callback validation.
    /// </summary>
    /// <param name="provider">OAuth provider (Google, Microsoft, GitHub).</param>
    /// <param name="scopes">Optional scopes to request. If not specified, uses the scopes from the provider configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the authorization URL.</response>
    /// <response code="400">Provider configuration not found.</response>
    [HttpGet("{provider}/authorize")]
    [Authorize]
    [ProducesResponseType<GetAuthorizationUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuthorizationUrl(
        [FromRoute] OAuthProvider provider,
        [FromQuery] List<string> scopes,
        CancellationToken cancellationToken)
    {
        try
        {
            var (authorizationUrl, state) = await _oauthFlowService.GenerateAuthorizationUrlAsync(
                _identityContext.UserId,
                provider,
                scopes.Count > 0 ? scopes : null,
                cancellationToken);

            return Ok(new GetAuthorizationUrlResponseV1
            {
                AuthorizationUrl = authorizationUrl,
                State = state
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate authorization URL for {Provider}", provider);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Handles the OAuth callback from the provider.
    /// Validates state against server-side store, exchanges code for tokens, and stores them.
    /// Redirects to frontend with success or error.
    /// </summary>
    /// <param name="provider">OAuth provider.</param>
    /// <param name="code">Authorization code from provider.</param>
    /// <param name="state">State parameter for CSRF validation.</param>
    /// <param name="error">Error code if authorization failed.</param>
    /// <param name="error_description">Error description if authorization failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="302">Redirects to frontend with result.</response>
    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromRoute] OAuthProvider provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken cancellationToken)
    {
        var frontendBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var callbackUrl = $"{frontendBaseUrl}/oauth/callback";

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth callback error from {Provider}: {Error}", provider, error);
            return Redirect($"{callbackUrl}?error={Uri.EscapeDataString(error)}&provider={provider}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback missing code or state for {Provider}", provider);
            return Redirect($"{callbackUrl}?error=missing_parameters&provider={provider}");
        }

        // Validate state against server-side store and get flow context
        var callbackState = await _oauthFlowService.ValidateAndConsumeStateAsync(state, cancellationToken);
        if (callbackState == null)
        {
            _logger.LogWarning("OAuth state invalid or expired for {Provider}", provider);
            return Redirect($"{callbackUrl}?error=invalid_state&provider={provider}");
        }

        // Verify the provider in the URL matches the state
        if (callbackState.Provider != provider)
        {
            _logger.LogWarning(
                "OAuth provider mismatch: URL has {UrlProvider} but state has {StateProvider}",
                provider, callbackState.Provider);
            return Redirect($"{callbackUrl}?error=provider_mismatch&provider={provider}");
        }

        // Populate identity context from the stored state so downstream services work
        _identityContext.SetIdentity(callbackState.UserId);

        try
        {
            await _oauthFlowService.HandleCallbackAsync(
                callbackState.UserId,
                callbackState.Provider,
                code,
                callbackState.CodeVerifier,
                cancellationToken);

            _logger.LogInformation("OAuth flow completed successfully for {Provider}", provider);
            return Redirect($"{callbackUrl}?success=true&provider={provider}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback handling failed for {Provider}", provider);
            return Redirect($"{callbackUrl}?error=callback_failed&provider={provider}");
        }
    }
}
