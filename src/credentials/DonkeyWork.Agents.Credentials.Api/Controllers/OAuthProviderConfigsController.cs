using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Enums;
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
    /// Returns metadata for all supported OAuth providers including endpoint URLs,
    /// default scopes, and setup instructions.
    /// </summary>
    /// <response code="200">Returns provider metadata.</response>
    [HttpGet("providers")]
    [ProducesResponseType<IReadOnlyList<OAuthProviderMetadataV1>>(StatusCodes.Status200OK)]
    public IActionResult GetProviderMetadata()
    {
        var metadata = new List<OAuthProviderMetadataV1>
        {
            new()
            {
                Provider = OAuthProvider.Google,
                DisplayName = "Google",
                AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenUrl = "https://oauth2.googleapis.com/token",
                UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo",
                DefaultScopes = ["openid", "profile", "email", "https://www.googleapis.com/auth/gmail.readonly", "https://www.googleapis.com/auth/drive.file"],
                SetupUrl = "https://console.cloud.google.com/apis/credentials",
                SetupInstructions = "Create a project in Google Cloud Console, enable the Gmail and Drive APIs, then create OAuth 2.0 credentials under APIs & Services > Credentials. Set the authorized redirect URI to the callback URL shown below.",
                IsBuiltIn = true,
                AvailableScopes =
                [
                    new() { Name = "openid", Description = "Verify your identity", IsRequired = true, IsDefault = true },
                    new() { Name = "profile", Description = "View your basic profile info", IsRequired = true, IsDefault = true },
                    new() { Name = "email", Description = "View your email address", IsRequired = true, IsDefault = true },
                    new() { Name = "https://www.googleapis.com/auth/gmail.readonly", Description = "Read your Gmail messages", IsRequired = false, IsDefault = true },
                    new() { Name = "https://www.googleapis.com/auth/gmail.send", Description = "Send emails on your behalf", IsRequired = false, IsDefault = false },
                    new() { Name = "https://www.googleapis.com/auth/drive.file", Description = "Access files you open or create with this app", IsRequired = false, IsDefault = true },
                    new() { Name = "https://www.googleapis.com/auth/drive.readonly", Description = "View all your Google Drive files", IsRequired = false, IsDefault = false },
                    new() { Name = "https://www.googleapis.com/auth/calendar.readonly", Description = "View your calendar events", IsRequired = false, IsDefault = false },
                    new() { Name = "https://www.googleapis.com/auth/calendar.events", Description = "Manage your calendar events", IsRequired = false, IsDefault = false },
                ]
            },
            new()
            {
                Provider = OAuthProvider.Microsoft,
                DisplayName = "Microsoft",
                AuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                UserInfoUrl = "https://graph.microsoft.com/v1.0/me",
                DefaultScopes = ["openid", "offline_access", "profile", "email", "User.Read", "Mail.Read", "Files.ReadWrite.All"],
                SetupUrl = "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade",
                SetupInstructions = "Register an application in Azure Portal under App registrations. Add a Web platform with the redirect URI shown below. Create a client secret under Certificates & secrets. Grant the required Microsoft Graph API permissions.",
                IsBuiltIn = true,
                AvailableScopes =
                [
                    new() { Name = "openid", Description = "Sign you in", IsRequired = true, IsDefault = true },
                    new() { Name = "offline_access", Description = "Maintain access when you're not using the app", IsRequired = true, IsDefault = true },
                    new() { Name = "profile", Description = "View your basic profile", IsRequired = true, IsDefault = true },
                    new() { Name = "email", Description = "View your email address", IsRequired = true, IsDefault = true },
                    new() { Name = "User.Read", Description = "Read your user profile", IsRequired = true, IsDefault = true },
                    new() { Name = "Mail.Read", Description = "Read your mail", IsRequired = false, IsDefault = true },
                    new() { Name = "Mail.Send", Description = "Send mail on your behalf", IsRequired = false, IsDefault = false },
                    new() { Name = "Files.ReadWrite.All", Description = "Read and write all your files", IsRequired = false, IsDefault = true },
                    new() { Name = "Calendars.Read", Description = "Read your calendars", IsRequired = false, IsDefault = false },
                    new() { Name = "Calendars.ReadWrite", Description = "Read and write your calendars", IsRequired = false, IsDefault = false },
                ]
            },
            new()
            {
                Provider = OAuthProvider.GitHub,
                DisplayName = "GitHub",
                AuthorizationUrl = "https://github.com/login/oauth/authorize",
                TokenUrl = "https://github.com/login/oauth/access_token",
                UserInfoUrl = "https://api.github.com/user",
                DefaultScopes = ["user:email", "repo"],
                SetupUrl = "https://github.com/settings/developers",
                SetupInstructions = "Go to GitHub Settings > Developer settings > OAuth Apps and create a new OAuth App. Set the Authorization callback URL to the redirect URI shown below.",
                IsBuiltIn = true,
                AvailableScopes =
                [
                    new() { Name = "user:email", Description = "Access your email addresses", IsRequired = true, IsDefault = true },
                    new() { Name = "repo", Description = "Full control of private repositories", IsRequired = false, IsDefault = true },
                    new() { Name = "read:org", Description = "Read organization membership", IsRequired = false, IsDefault = false },
                    new() { Name = "gist", Description = "Create and manage gists", IsRequired = false, IsDefault = false },
                    new() { Name = "notifications", Description = "Access your notifications", IsRequired = false, IsDefault = false },
                    new() { Name = "read:packages", Description = "Read packages from GitHub Packages", IsRequired = false, IsDefault = false },
                ]
            },
            new()
            {
                Provider = OAuthProvider.Custom,
                DisplayName = "Custom",
                AuthorizationUrl = "",
                TokenUrl = "",
                UserInfoUrl = "",
                DefaultScopes = [],
                SetupUrl = "",
                SetupInstructions = "Enter the OAuth 2.0 authorization and token endpoint URLs from your provider. The user info endpoint is optional but recommended for identifying connected accounts.",
                IsBuiltIn = false,
                AvailableScopes = []
            }
        };

        return Ok(metadata);
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
            HasToken = tokens.Any(t => t.Provider == c.Provider),
            CustomProviderName = c.CustomProviderName,
            Scopes = c.Scopes
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
            CreatedAt = config.CreatedAt,
            AuthorizationUrl = config.AuthorizationUrl,
            TokenUrl = config.TokenUrl,
            UserInfoUrl = config.UserInfoUrl,
            Scopes = config.Scopes,
            CustomProviderName = config.CustomProviderName
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
                request.AuthorizationUrl,
                request.TokenUrl,
                request.UserInfoUrl,
                request.Scopes,
                request.CustomProviderName,
                cancellationToken);

            var item = new OAuthProviderConfigItemV1
            {
                Id = config.Id,
                Provider = config.Provider,
                RedirectUri = config.RedirectUri,
                CreatedAt = config.CreatedAt,
                HasToken = false,
                CustomProviderName = config.CustomProviderName,
                Scopes = config.Scopes
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
                request.AuthorizationUrl,
                request.TokenUrl,
                request.UserInfoUrl,
                request.Scopes,
                request.CustomProviderName,
                cancellationToken);

            var detail = new OAuthProviderConfigDetailV1
            {
                Id = config.Id,
                Provider = config.Provider,
                ClientId = MaskSecret(config.ClientId),
                ClientSecret = MaskSecret(config.ClientSecret),
                RedirectUri = config.RedirectUri,
                CreatedAt = config.CreatedAt,
                AuthorizationUrl = config.AuthorizationUrl,
                TokenUrl = config.TokenUrl,
                UserInfoUrl = config.UserInfoUrl,
                Scopes = config.Scopes,
                CustomProviderName = config.CustomProviderName
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
