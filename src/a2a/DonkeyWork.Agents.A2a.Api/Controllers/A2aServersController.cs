using Asp.Versioning;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.A2a.Api.Controllers;

/// <summary>
/// Manage A2A server configurations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/a2a-servers")]
[Authorize]
[Produces("application/json")]
public class A2aServersController : ControllerBase
{
    private readonly IA2aServerConfigurationService _a2aServerConfigurationService;
    private readonly IA2aServerTestService _a2aServerTestService;

    public A2aServersController(
        IA2aServerConfigurationService a2aServerConfigurationService,
        IA2aServerTestService a2aServerTestService)
    {
        _a2aServerConfigurationService = a2aServerConfigurationService;
        _a2aServerTestService = a2aServerTestService;
    }

    /// <summary>
    /// Create a new A2A server configuration.
    /// </summary>
    /// <param name="request">The A2A server configuration details.</param>
    /// <response code="201">Returns the created A2A server configuration.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<A2aServerDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateA2aServerRequestV1 request)
    {
        var config = await _a2aServerConfigurationService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = config.Id }, config);
    }

    /// <summary>
    /// Get a specific A2A server configuration by ID.
    /// </summary>
    /// <param name="id">The A2A server configuration ID.</param>
    /// <response code="200">Returns the A2A server configuration.</response>
    /// <response code="404">A2A server configuration not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<A2aServerDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var config = await _a2aServerConfigurationService.GetByIdAsync(id);

        if (config == null)
            return NotFound();

        return Ok(config);
    }

    /// <summary>
    /// List all A2A server configurations for the current user.
    /// </summary>
    /// <response code="200">Returns the list of A2A server configurations.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<A2aServerSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination)
    {
        var configs = await _a2aServerConfigurationService.ListAsync(pagination);
        return Ok(configs);
    }

    /// <summary>
    /// Update an A2A server configuration.
    /// </summary>
    /// <param name="id">The A2A server configuration ID.</param>
    /// <param name="request">The updated A2A server configuration details.</param>
    /// <response code="200">Returns the updated A2A server configuration.</response>
    /// <response code="404">A2A server configuration not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<A2aServerDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateA2aServerRequestV1 request)
    {
        var config = await _a2aServerConfigurationService.UpdateAsync(id, request);

        if (config == null)
            return NotFound();

        return Ok(config);
    }

    /// <summary>
    /// Delete an A2A server configuration.
    /// </summary>
    /// <param name="id">The A2A server configuration ID.</param>
    /// <response code="204">A2A server configuration deleted.</response>
    /// <response code="404">A2A server configuration not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _a2aServerConfigurationService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Discover an A2A server by fetching its agent card. No auth headers are sent.
    /// </summary>
    [HttpPost("discover")]
    [ProducesResponseType<TestA2aServerResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Discover([FromBody] DiscoverA2aServerRequestV1 request)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
            return BadRequest("Address is required.");

        var result = await _a2aServerTestService.DiscoverAsync(request.Address);
        return Ok(result);
    }

    /// <summary>
    /// Test connection to an A2A server and retrieve the agent card.
    /// </summary>
    /// <param name="id">The A2A server configuration ID.</param>
    /// <response code="200">Returns the test result (success or failure in body).</response>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType<TestA2aServerResponseV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Test(Guid id)
    {
        var result = await _a2aServerTestService.TestConnectionAsync(id);
        return Ok(result);
    }
}
