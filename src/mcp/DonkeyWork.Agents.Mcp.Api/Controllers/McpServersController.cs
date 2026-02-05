using Asp.Versioning;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Mcp.Api.Controllers;

/// <summary>
/// Manage MCP server configurations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/mcp-servers")]
[Authorize]
[Produces("application/json")]
public class McpServersController : ControllerBase
{
    private readonly IMcpServerConfigurationService _mcpServerConfigurationService;

    public McpServersController(IMcpServerConfigurationService mcpServerConfigurationService)
    {
        _mcpServerConfigurationService = mcpServerConfigurationService;
    }

    /// <summary>
    /// Create a new MCP server configuration.
    /// </summary>
    /// <param name="request">The MCP server configuration details.</param>
    /// <response code="201">Returns the created MCP server configuration.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<McpServerDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMcpServerRequestV1 request)
    {
        var config = await _mcpServerConfigurationService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = config.Id }, config);
    }

    /// <summary>
    /// Get a specific MCP server configuration by ID.
    /// </summary>
    /// <param name="id">The MCP server configuration ID.</param>
    /// <response code="200">Returns the MCP server configuration.</response>
    /// <response code="404">MCP server configuration not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<McpServerDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var config = await _mcpServerConfigurationService.GetByIdAsync(id);

        if (config == null)
            return NotFound();

        return Ok(config);
    }

    /// <summary>
    /// List all MCP server configurations for the current user.
    /// </summary>
    /// <response code="200">Returns the list of MCP server configurations.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<McpServerSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var configs = await _mcpServerConfigurationService.ListAsync();
        return Ok(configs);
    }

    /// <summary>
    /// Update an MCP server configuration.
    /// </summary>
    /// <param name="id">The MCP server configuration ID.</param>
    /// <param name="request">The updated MCP server configuration details.</param>
    /// <response code="200">Returns the updated MCP server configuration.</response>
    /// <response code="404">MCP server configuration not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<McpServerDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMcpServerRequestV1 request)
    {
        var config = await _mcpServerConfigurationService.UpdateAsync(id, request);

        if (config == null)
            return NotFound();

        return Ok(config);
    }

    /// <summary>
    /// Delete an MCP server configuration.
    /// </summary>
    /// <param name="id">The MCP server configuration ID.</param>
    /// <response code="204">MCP server configuration deleted.</response>
    /// <response code="404">MCP server configuration not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mcpServerConfigurationService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
