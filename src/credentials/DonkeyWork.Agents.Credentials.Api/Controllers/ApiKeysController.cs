using Asp.Versioning;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

using DonkeyWork.Agents.Common.Contracts.Models.Pagination;

/// <summary>
/// Manage API keys for programmatic access.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/apikeys")]
[Authorize]
[Produces("application/json")]
public class ApiKeysController : ControllerBase
{
    private readonly IUserApiKeyService _apiKeyService;

    public ApiKeysController(IUserApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// List all API keys for the current user.
    /// </summary>
    /// <response code="200">Returns the list of API keys with masked key values.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ApiKeyItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination)
    {
        var limit = pagination.GetLimit();
        var (keys, totalCount) = await _apiKeyService.ListAsync(pagination.Offset, limit);

        var response = new PaginatedResponse<ApiKeyItemV1>
        {
            Items = keys.Select(k => new ApiKeyItemV1
            {
                Id = k.Id,
                Name = k.Name,
                Description = k.Description,
                MaskedKey = k.Key,
                CreatedAt = k.CreatedAt
            }).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific API key with full (unmasked) value.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <response code="200">Returns the API key with full value.</response>
    /// <response code="404">API key not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetApiKeyResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var key = await _apiKeyService.GetByIdAsync(id);

        if (key == null)
            return NotFound();

        var response = new GetApiKeyResponseV1
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            Key = key.Key,
            CreatedAt = key.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new API key.
    /// </summary>
    /// <param name="request">The API key details.</param>
    /// <response code="200">Returns the created API key with full value (only shown once).</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<CreateApiKeyResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequestV1 request)
    {
        var key = await _apiKeyService.CreateAsync(request.Name, request.Description);

        var response = new CreateApiKeyResponseV1
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            Key = key.Key,
            CreatedAt = key.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete an API key.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <response code="204">API key deleted.</response>
    /// <response code="404">API key not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _apiKeyService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
