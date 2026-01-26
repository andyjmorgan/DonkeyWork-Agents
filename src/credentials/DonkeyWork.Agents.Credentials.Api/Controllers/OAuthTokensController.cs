using Asp.Versioning;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Manages OAuth tokens for connected accounts.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/oauth/tokens")]
[Authorize]
[Produces("application/json")]
public class OAuthTokensController : ControllerBase
{
    private readonly IOAuthTokenService _tokenService;
    private readonly IOAuthProviderConfigService _configService;
    private readonly IOAuthProviderFactory _providerFactory;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<OAuthTokensController> _logger;

    public OAuthTokensController(
        IOAuthTokenService tokenService,
        IOAuthProviderConfigService configService,
        IOAuthProviderFactory providerFactory,
        IIdentityContext identityContext,
        ILogger<OAuthTokensController> logger)
    {
        _tokenService = tokenService;
        _configService = configService;
        _providerFactory = providerFactory;
        _identityContext = identityContext;
        _logger = logger;
    }

    /// <summary>
    /// Lists all OAuth tokens for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of OAuth tokens.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OAuthTokenItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tokens = await _tokenService.GetByUserIdAsync(_identityContext.UserId, cancellationToken);

        var items = tokens.Select(t => new OAuthTokenItemV1
        {
            Id = t.Id,
            Provider = t.Provider,
            Email = t.Email,
            ExternalUserId = t.ExternalUserId,
            Status = GetTokenStatus(t.ExpiresAt),
            ExpiresAt = t.ExpiresAt,
            LastRefreshedAt = t.LastRefreshedAt,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Ok(items);
    }

    /// <summary>
    /// Gets a specific OAuth token by ID.
    /// </summary>
    /// <param name="id">Token ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the OAuth token details.</response>
    /// <response code="404">Token not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<OAuthTokenDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetByIdAsync(_identityContext.UserId, id, cancellationToken);

        if (token == null)
        {
            return NotFound();
        }

        var detail = new OAuthTokenDetailV1
        {
            Id = token.Id,
            Provider = token.Provider,
            Email = token.Email,
            ExternalUserId = token.ExternalUserId,
            AccessToken = MaskToken(token.AccessToken),
            Scopes = token.Scopes,
            Status = GetTokenStatus(token.ExpiresAt),
            ExpiresAt = token.ExpiresAt,
            LastRefreshedAt = token.LastRefreshedAt,
            CreatedAt = token.CreatedAt
        };

        return Ok(detail);
    }

    /// <summary>
    /// Manually refreshes an OAuth token.
    /// </summary>
    /// <param name="id">Token ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="404">Token not found.</response>
    /// <response code="400">Refresh failed.</response>
    [HttpPost("{id}/refresh")]
    [ProducesResponseType<RefreshTokenResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetByIdAsync(_identityContext.UserId, id, cancellationToken);

        if (token == null)
        {
            return NotFound();
        }

        try
        {
            // Get provider config
            var config = await _configService.GetByProviderAsync(_identityContext.UserId, token.Provider, cancellationToken);
            if (config == null)
            {
                return BadRequest(new RefreshTokenResponseV1
                {
                    Success = false,
                    Error = "Provider configuration not found"
                });
            }

            // Get provider instance
            var provider = _providerFactory.GetProvider(token.Provider);

            // Refresh token
            var tokenResponse = await provider.RefreshTokenAsync(
                token.RefreshToken,
                config.ClientId,
                config.ClientSecret,
                cancellationToken);

            // Calculate new expiration
            var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            // Update stored token
            await _tokenService.RefreshTokenAsync(
                id,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? token.RefreshToken,
                newExpiresAt,
                cancellationToken);

            _logger.LogInformation("Token {TokenId} refreshed successfully", id);

            return Ok(new RefreshTokenResponseV1
            {
                Success = true,
                ExpiresAt = newExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token {TokenId}", id);
            return BadRequest(new RefreshTokenResponseV1
            {
                Success = false,
                Error = "Token refresh failed"
            });
        }
    }

    /// <summary>
    /// Disconnects an OAuth account by deleting the token.
    /// </summary>
    /// <param name="id">Token ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Token deleted successfully.</response>
    /// <response code="404">Token not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _tokenService.DeleteAsync(_identityContext.UserId, id, cancellationToken);
            _logger.LogInformation("Token {TokenId} disconnected", id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static OAuthTokenStatus GetTokenStatus(DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        var timeUntilExpiry = expiresAt - now;

        if (timeUntilExpiry <= TimeSpan.Zero)
        {
            return OAuthTokenStatus.Expired;
        }

        if (timeUntilExpiry <= TimeSpan.FromMinutes(10))
        {
            return OAuthTokenStatus.ExpiringSoon;
        }

        return OAuthTokenStatus.Active;
    }

    private static string MaskToken(string token)
    {
        if (token.Length <= 12)
        {
            return "***";
        }

        var prefix = token[..6];
        var suffix = token[^6..];
        return $"{prefix}...{suffix}";
    }
}
