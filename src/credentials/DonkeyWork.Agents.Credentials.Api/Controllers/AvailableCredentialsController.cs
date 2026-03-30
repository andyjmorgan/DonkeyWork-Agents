using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Lists available credential types and their configuration status.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/credentials/available")]
[Authorize]
[Produces("application/json")]
public class AvailableCredentialsController : ControllerBase
{
    private readonly IOAuthTokenService _tokenService;
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly IIdentityContext _identityContext;

    public AvailableCredentialsController(
        IOAuthTokenService tokenService,
        IExternalApiKeyService externalApiKeyService,
        IIdentityContext identityContext)
    {
        _tokenService = tokenService;
        _externalApiKeyService = externalApiKeyService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// Lists all available credential types with the user's configuration status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns all credential types with configuration status.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AvailableCredentialV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = _identityContext.UserId;

        var oauthTokensTask = _tokenService.GetByUserIdAsync(userId, cancellationToken);
        var apiKeyProvidersTask = _externalApiKeyService.GetConfiguredLlmProvidersAsync(userId, cancellationToken);

        await Task.WhenAll(oauthTokensTask, apiKeyProvidersTask);

        var oauthTokens = await oauthTokensTask;
        var configuredApiKeyProviders = await apiKeyProvidersTask;

        var configuredOAuthProviders = oauthTokens.Select(t => t.Provider).ToHashSet();

        var result = new List<AvailableCredentialV1>();

        foreach (var provider in Enum.GetValues<OAuthProvider>())
        {
            result.Add(new AvailableCredentialV1
            {
                CredentialType = "OAuth",
                Provider = provider.ToString(),
                IsConfigured = configuredOAuthProviders.Contains(provider)
            });
        }

        foreach (var provider in Enum.GetValues<ExternalApiKeyProvider>())
        {
            result.Add(new AvailableCredentialV1
            {
                CredentialType = "ApiKey",
                Provider = provider.ToString(),
                IsConfigured = configuredApiKeyProviders.Contains(provider)
            });
        }

        return Ok(result);
    }
}
