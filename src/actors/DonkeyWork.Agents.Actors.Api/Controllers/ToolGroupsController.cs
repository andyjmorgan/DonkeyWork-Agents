using Asp.Versioning;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Actors.Api.Controllers;

/// <summary>
/// Endpoints for retrieving available tool groups and their individual tools.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tool-groups")]
[Authorize]
[Produces("application/json")]
public class ToolGroupsController : ControllerBase
{
    private readonly IToolGroupService _toolGroupService;

    public ToolGroupsController(IToolGroupService toolGroupService)
    {
        _toolGroupService = toolGroupService;
    }

    /// <summary>
    /// Gets all available tool groups with their individual tools.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ToolGroupDefinitionV1>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAll()
    {
        var groups = _toolGroupService.GetAll();
        return Ok(groups);
    }
}
