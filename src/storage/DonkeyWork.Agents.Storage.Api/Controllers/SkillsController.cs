using Asp.Versioning;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Storage.Api.Controllers;

/// <summary>
/// Manage per-user skills (zip upload / list / delete).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/skills")]
[Authorize]
[Produces("application/json")]
public class SkillsController : ControllerBase
{
    private readonly ISkillsService _skillsService;

    public SkillsController(ISkillsService skillsService)
    {
        _skillsService = skillsService;
    }

    /// <summary>
    /// List all skills for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<SkillItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var skills = await _skillsService.ListAsync();
        return Ok(skills);
    }

    /// <summary>
    /// Upload a new skill as a zip archive.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SkillUploadResultV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "File is empty." });

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .zip files are accepted." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _skillsService.UploadAsync(stream);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a skill by name.
    /// </summary>
    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string name)
    {
        var deleted = await _skillsService.DeleteAsync(name);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
