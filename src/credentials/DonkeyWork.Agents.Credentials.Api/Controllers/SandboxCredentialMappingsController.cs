using Asp.Versioning;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Manage sandbox credential mappings that map domains to credential headers.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sandbox-credential-mappings")]
[Authorize]
[Produces("application/json")]
public class SandboxCredentialMappingsController : ControllerBase
{
    private readonly ISandboxCredentialMappingService _service;

    public SandboxCredentialMappingsController(ISandboxCredentialMappingService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all sandbox credential mappings for the current user.
    /// </summary>
    /// <response code="200">Returns the list of sandbox credential mappings.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SandboxCredentialMappingV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var mappings = await _service.ListAsync();
        return Ok(mappings);
    }

    /// <summary>
    /// Get a specific sandbox credential mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <response code="200">Returns the sandbox credential mapping.</response>
    /// <response code="404">Mapping not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SandboxCredentialMappingV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var mapping = await _service.GetByIdAsync(id);

        if (mapping is null)
            return NotFound();

        return Ok(mapping);
    }

    /// <summary>
    /// Create a new sandbox credential mapping.
    /// </summary>
    /// <param name="request">The mapping details.</param>
    /// <response code="201">Returns the created mapping.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<SandboxCredentialMappingV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSandboxCredentialMappingRequestV1 request)
    {
        var mapping = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = mapping.Id }, mapping);
    }

    /// <summary>
    /// Update a sandbox credential mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The updated mapping details.</param>
    /// <response code="200">Returns the updated mapping.</response>
    /// <response code="404">Mapping not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<SandboxCredentialMappingV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSandboxCredentialMappingRequestV1 request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        var mapping = await _service.UpdateAsync(id, request);
        return Ok(mapping);
    }

    /// <summary>
    /// Delete a sandbox credential mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <response code="204">Mapping deleted.</response>
    /// <response code="404">Mapping not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get the list of domains that have credential mappings configured.
    /// </summary>
    /// <response code="200">Returns the list of configured domains.</response>
    [HttpGet("domains")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfiguredDomains()
    {
        var domains = await _service.GetConfiguredDomainsAsync();
        return Ok(domains);
    }
}
