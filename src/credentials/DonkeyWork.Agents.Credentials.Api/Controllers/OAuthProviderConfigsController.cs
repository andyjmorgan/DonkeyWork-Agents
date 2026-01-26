using Asp.Versioning;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Manages OAuth provider configurations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/oauth/configs")]
[Authorize]
[Produces("application/json")]
public class OAuthProviderConfigsController : ControllerBase
{
    private readonly IOAuthProviderConfigService _configService;
    private readonly IOAuthTokenService _tokenService;
    private readonly IIdentityContext _identityContext;

    public OAuthProviderConfigsController(
        IOAuthProviderConfigService configService,
        IOAuthTokenService tokenService,
        IIdentityContext identityContext)
    {
        _configService = configService;
        _tokenService = tokenService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// Lists all OAuth provider configurations for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of provider configurations.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OAuthProviderConfigItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var configs = await _configService.GetByUserIdAsync(_identityContext.UserId, cancellationToken);
        var tokens = await _tokenService.GetByUserIdAsync(_identityContext.UserId, cancellationToken);

        var items = configs.Select(c => new OAuthProviderConfigItemV1
        {
            Id = c.Id,
            Provider = c.Provider,
            RedirectUri = c.RedirectUri,
            CreatedAt = c.CreatedAt,
            HasToken = tokens.Any(t => t.Provider == c.Provider)
        }).ToList();

        return Ok(items);
    }

    /// <summary>
    /// Gets a specific OAuth provider configuration by ID.
    /// </summary>
    /// <param name="id">Configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the provider configuration.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<OAuthProviderConfigDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var config = await _configService.GetByIdAsync(_identityContext.UserId, id, cancellationToken);

        if (config == null)
        {
            return NotFound();
        }

        var detail = new OAuthProviderConfigDetailV1
        {
            Id = config.Id,
            Provider = config.Provider,
            ClientId = MaskSecret(config.ClientId),
            ClientSecret = MaskSecret(config.ClientSecret),
            RedirectUri = config.RedirectUri,
            CreatedAt = config.CreatedAt
        };

        return Ok(detail);
    }

    /// <summary>
    /// Creates a new OAuth provider configuration.
    /// </summary>
    /// <param name="request">Configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Configuration created successfully.</response>
    /// <response code="400">Invalid request or provider already configured.</response>
    [HttpPost]
    [ProducesResponseType<OAuthProviderConfigItemV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOAuthProviderConfigRequestV1 request,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configService.CreateAsync(
                _identityContext.UserId,
                request.Provider,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                cancellationToken);

            var item = new OAuthProviderConfigItemV1
            {
                Id = config.Id,
                Provider = config.Provider,
                RedirectUri = config.RedirectUri,
                CreatedAt = config.CreatedAt,
                HasToken = false
            };

            return CreatedAtAction(nameof(Get), new { id = config.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing OAuth provider configuration.
    /// </summary>
    /// <param name="id">Configuration ID.</param>
    /// <param name="request">Updated configuration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Configuration updated successfully.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType<OAuthProviderConfigDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateOAuthProviderConfigRequestV1 request,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configService.UpdateAsync(
                _identityContext.UserId,
                id,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                cancellationToken);

            var detail = new OAuthProviderConfigDetailV1
            {
                Id = config.Id,
                Provider = config.Provider,
                ClientId = MaskSecret(config.ClientId),
                ClientSecret = MaskSecret(config.ClientSecret),
                RedirectUri = config.RedirectUri,
                CreatedAt = config.CreatedAt
            };

            return Ok(detail);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes an OAuth provider configuration.
    /// </summary>
    /// <param name="id">Configuration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Configuration deleted successfully.</response>
    /// <response code="404">Configuration not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _configService.DeleteAsync(_identityContext.UserId, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private static string MaskSecret(string secret)
    {
        if (secret.Length <= 8)
        {
            return "***";
        }

        var prefix = secret[..4];
        var suffix = secret[^4..];
        return $"{prefix}***{suffix}";
    }
}
