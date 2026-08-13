using Asp.Versioning;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Actors.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/navi/settings")]
[Authorize]
[Produces("application/json")]
public sealed class NaviSettingsController : ControllerBase
{
    private readonly INaviSettingsService _service;

    public NaviSettingsController(INaviSettingsService service) => _service = service;

    [HttpGet]
    [ProducesResponseType<NaviSettingsV1>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NaviSettingsV1>> Get(CancellationToken cancellationToken) =>
        Ok(await _service.GetAsync(cancellationToken));

    [HttpPut]
    [ProducesResponseType<NaviSettingsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NaviSettingsV1>> Update(
        UpdateNaviSettingsRequestV1 request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpdateAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
