using Asp.Versioning;
using DonkeyWork.Agents.Actions.Contracts.Models.Api;
using DonkeyWork.Agents.Actions.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Actions.Api.Controllers;

/// <summary>
/// Controller for managing and executing action nodes
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ActionsController : ControllerBase
{
    private readonly IActionSchemaService _schemaService;
    private readonly ILogger<ActionsController> _logger;

    public ActionsController(
        IActionSchemaService schemaService,
        ILogger<ActionsController> logger)
    {
        _schemaService = schemaService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available action node schemas
    /// </summary>
    /// <returns>List of action schemas with parameter definitions</returns>
    /// <response code="200">Returns the list of action schemas</response>
    [HttpGet("schemas")]
    [ProducesResponseType<GetSchemasResponseV1>(StatusCodes.Status200OK)]
    public IActionResult GetSchemas()
    {
        try
        {
            // Get the assembly containing action providers
            var assembly = typeof(DonkeyWork.Agents.Actions.Core.Providers.HttpActionProvider).Assembly;

            // Generate schemas for all action nodes in the assembly
            var schemas = _schemaService.GenerateSchemas(assembly);

            var response = new GetSchemasResponseV1
            {
                Schemas = schemas,
                Count = schemas.Count
            };

            _logger.LogInformation("Generated {Count} action schemas", schemas.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating action schemas");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to generate action schemas");
        }
    }

    // TODO: Phase 4 - Add execution endpoints
    // POST /api/v1/actions/execute
    // POST /api/v1/actions/validate
}
