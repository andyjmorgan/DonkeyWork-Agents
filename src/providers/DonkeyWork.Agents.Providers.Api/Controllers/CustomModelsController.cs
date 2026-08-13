using Asp.Versioning;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Providers.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/custom-models")]
[Authorize]
[Produces("application/json")]
public sealed class CustomModelsController : ControllerBase
{
    private readonly ICustomModelService _service;
    public CustomModelsController(ICustomModelService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomModelV1>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomModelV1>> Get(Guid id, CancellationToken cancellationToken)
    {
        var model = await _service.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<CustomModelV1>> Create(CreateCustomModelRequestV1 request, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = model.Id, version = "1" }, model);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomModelV1>> Update(Guid id, UpdateCustomModelRequestV1 request, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _service.UpdateAsync(id, request, cancellationToken);
            return model is null ? NotFound() : Ok(model);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("test")]
    public async Task<ActionResult<TestCustomModelResponseV1>> Test(TestCustomModelRequestV1 request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.TestAsync(request, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new TestCustomModelResponseV1 { Success = false, Message = ex.Message }); }
    }
}
