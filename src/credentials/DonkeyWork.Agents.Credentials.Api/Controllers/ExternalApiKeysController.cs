using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Manage external API keys for third-party services (OpenAI, Anthropic, Google, etc.).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/credentials")]
[Authorize]
[Produces("application/json")]
public class ExternalApiKeysController : ControllerBase
{
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly IIdentityContext _identityContext;

    public ExternalApiKeysController(
        IExternalApiKeyService externalApiKeyService,
        IIdentityContext identityContext)
    {
        _externalApiKeyService = externalApiKeyService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// List all external API keys for the current user.
    /// </summary>
    /// <response code="200">Returns the list of external API keys with masked values.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ExternalApiKeyItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination)
    {
        var keys = await _externalApiKeyService.GetByUserIdAsync(_identityContext.UserId);

        var limit = pagination.GetLimit();
        var pagedKeys = keys
            .Skip(pagination.Offset)
            .Take(limit)
            .ToList();

        var response = new PaginatedResponse<ExternalApiKeyItemV1>
        {
            Items = pagedKeys.Select(k => new ExternalApiKeyItemV1
            {
                Id = k.Id,
                Provider = k.Provider,
                Name = k.Name,
                MaskedApiKey = k.Fields.GetValueOrDefault(CredentialFieldType.ApiKey) ?? "***",
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt
            }).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = keys.Count
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific external API key with full (unmasked) value.
    /// </summary>
    /// <param name="id">The external API key ID.</param>
    /// <response code="200">Returns the external API key with full value.</response>
    /// <response code="404">External API key not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ExternalApiKeyResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var key = await _externalApiKeyService.GetByIdAsync(_identityContext.UserId, id);

        if (key == null)
            return NotFound();

        var response = new ExternalApiKeyResponseV1
        {
            Id = key.Id,
            Provider = key.Provider,
            Name = key.Name,
            ApiKey = key.Fields.GetValueOrDefault(CredentialFieldType.ApiKey) ?? string.Empty,
            CreatedAt = key.CreatedAt,
            LastUsedAt = key.LastUsedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new external API key.
    /// </summary>
    /// <param name="request">The external API key details.</param>
    /// <response code="201">Returns the created external API key.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<ExternalApiKeyResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExternalApiKeyRequestV1 request)
    {
        var fields = new Dictionary<CredentialFieldType, string>
        {
            { CredentialFieldType.ApiKey, request.ApiKey }
        };

        var key = await _externalApiKeyService.CreateAsync(
            _identityContext.UserId,
            request.Provider,
            request.Name,
            fields);

        var response = new ExternalApiKeyResponseV1
        {
            Id = key.Id,
            Provider = key.Provider,
            Name = key.Name,
            ApiKey = key.Fields.GetValueOrDefault(CredentialFieldType.ApiKey) ?? string.Empty,
            CreatedAt = key.CreatedAt,
            LastUsedAt = key.LastUsedAt
        };

        return CreatedAtAction(nameof(Get), new { id = key.Id }, response);
    }

    /// <summary>
    /// Delete an external API key.
    /// </summary>
    /// <param name="id">The external API key ID.</param>
    /// <response code="204">External API key deleted.</response>
    /// <response code="404">External API key not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _externalApiKeyService.GetByIdAsync(_identityContext.UserId, id);

        if (existing == null)
            return NotFound();

        await _externalApiKeyService.DeleteAsync(_identityContext.UserId, id);

        return NoContent();
    }

    /// <summary>
    /// List external API keys by provider.
    /// </summary>
    /// <param name="provider">The provider to filter by.</param>
    /// <response code="200">Returns the list of external API keys for the provider.</response>
    [HttpGet("provider/{provider}")]
    [ProducesResponseType<IReadOnlyList<ExternalApiKeyItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByProvider(ExternalApiKeyProvider provider)
    {
        var keys = await _externalApiKeyService.GetByProviderAsync(_identityContext.UserId, provider);

        var items = keys.Select(k => new ExternalApiKeyItemV1
        {
            Id = k.Id,
            Provider = k.Provider,
            Name = k.Name,
            MaskedApiKey = k.Fields.GetValueOrDefault(CredentialFieldType.ApiKey) ?? "***",
            CreatedAt = k.CreatedAt,
            LastUsedAt = k.LastUsedAt
        }).ToList();

        return Ok(items);
    }

    /// <summary>
    /// Get which LLM providers the user has configured credentials for.
    /// </summary>
    /// <response code="200">Returns the list of configured LLM providers.</response>
    [HttpGet("llm-providers")]
    [ProducesResponseType<IReadOnlyList<ExternalApiKeyProvider>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfiguredLlmProviders()
    {
        var providers = await _externalApiKeyService.GetConfiguredLlmProvidersAsync(_identityContext.UserId);
        return Ok(providers);
    }
}
