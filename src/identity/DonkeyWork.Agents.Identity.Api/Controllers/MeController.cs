using Asp.Versioning;
using DonkeyWork.Agents.Identity.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Identity.Api.Controllers;

/// <summary>
/// Endpoints for retrieving the authenticated user's information.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class MeController : ControllerBase
{
    private readonly IIdentityContext _identityContext;

    public MeController(IIdentityContext identityContext)
    {
        _identityContext = identityContext;
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <returns>The authenticated user's details.</returns>
    /// <response code="200">Returns the user's information.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType<GetMeResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        var response = new GetMeResponseV1
        {
            UserId = _identityContext.UserId,
            Email = _identityContext.Email,
            Name = _identityContext.Name,
            Username = _identityContext.Username,
            IsAuthenticated = _identityContext.IsAuthenticated
        };

        return Ok(response);
    }
}
