using Asp.Versioning;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Agents.Api.Controllers;

/// <summary>
/// Get available node types and their configuration schemas.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/agents/node-types")]
[Authorize]
[Produces("application/json")]
public class NodeTypesController : ControllerBase
{
    private readonly INodeTypeSchemaService _nodeTypeSchemaService;

    public NodeTypesController(INodeTypeSchemaService nodeTypeSchemaService)
    {
        _nodeTypeSchemaService = nodeTypeSchemaService;
    }

    /// <summary>
    /// Get available node types and their configuration schemas.
    /// </summary>
    /// <returns>A list of available node types with their JSON schemas.</returns>
    /// <response code="200">Returns the list of node types.</response>
    [HttpGet]
    [ProducesResponseType<GetNodeTypesResponseV1>(StatusCodes.Status200OK)]
    public IActionResult GetNodeTypes()
    {
        var nodeTypes = _nodeTypeSchemaService.GetNodeTypes();

        return Ok(new GetNodeTypesResponseV1
        {
            NodeTypes = nodeTypes
        });
    }
}
