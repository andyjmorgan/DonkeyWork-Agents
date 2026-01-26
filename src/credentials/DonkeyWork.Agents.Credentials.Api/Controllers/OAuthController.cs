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
    /// Stores state and code verifier in secure cookies for callback validation.
    /// </summary>
    /// <param name="provider">OAuth provider (Google, Microsoft, GitHub).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the authorization URL.</response>
    /// <response code="400">Provider configuration not found.</response>
    [HttpGet("{provider}/authorize")]
    [Authorize]
    [ProducesResponseType<GetAuthorizationUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuthorizationUrl(
        [FromRoute] OAuthProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            var (authorizationUrl, state, codeVerifier) = await _oauthFlowService.GenerateAuthorizationUrlAsync(
                _identityContext.UserId,
                provider,
                cancellationToken);

            // Store state and code verifier in secure cookies
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10)
            };

            Response.Cookies.Append($"oauth_state_{provider}", state, cookieOptions);
            Response.Cookies.Append($"oauth_verifier_{provider}", codeVerifier, cookieOptions);
            Response.Cookies.Append($"oauth_userid_{provider}", _identityContext.UserId.ToString(), cookieOptions);

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
    /// Validates state, exchanges code for tokens, and stores them.
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

        // Check for errors from provider
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth callback error from {Provider}: {Error}", provider, error);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error={Uri.EscapeDataString(error)}&provider={provider}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback missing code or state for {Provider}", provider);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error=missing_parameters&provider={provider}");
        }

        // Validate state
        if (!Request.Cookies.TryGetValue($"oauth_state_{provider}", out var expectedState) ||
            state != expectedState)
        {
            _logger.LogWarning("OAuth state mismatch for {Provider}", provider);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error=state_mismatch&provider={provider}");
        }

        // Get code verifier
        if (!Request.Cookies.TryGetValue($"oauth_verifier_{provider}", out var codeVerifier) ||
            string.IsNullOrEmpty(codeVerifier))
        {
            _logger.LogWarning("OAuth code verifier not found for {Provider}", provider);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error=missing_verifier&provider={provider}");
        }

        // Get user ID
        if (!Request.Cookies.TryGetValue($"oauth_userid_{provider}", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("OAuth user ID not found for {Provider}", provider);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error=missing_userid&provider={provider}");
        }

        try
        {
            // Handle callback and store tokens
            await _oauthFlowService.HandleCallbackAsync(
                userId,
                provider,
                code,
                state,
                codeVerifier,
                cancellationToken);

            CleanupCookies(provider);
            _logger.LogInformation("OAuth flow completed successfully for {Provider}", provider);
            return Redirect($"{callbackUrl}?success=true&provider={provider}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback handling failed for {Provider}", provider);
            CleanupCookies(provider);
            return Redirect($"{callbackUrl}?error=callback_failed&provider={provider}");
        }
    }

    private void CleanupCookies(OAuthProvider provider)
    {
        Response.Cookies.Delete($"oauth_state_{provider}");
        Response.Cookies.Delete($"oauth_verifier_{provider}");
        Response.Cookies.Delete($"oauth_userid_{provider}");
    }
}
