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

    /// <summary>
    /// Get the file tree contents of a skill.
    /// </summary>
    [HttpGet("{name}/contents")]
    [ProducesResponseType<List<SkillFileNodeV1>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContents(string name)
    {
        var contents = await _skillsService.GetContentsAsync(name);
        if (contents is null)
            return NotFound();
        return Ok(contents);
    }

    /// <summary>
    /// Read the content of a file within a skill.
    /// </summary>
    [HttpGet("{name}/files/{**path}")]
    [ProducesResponseType<ReadFileResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReadFile(string name, string path)
    {
        var result = await _skillsService.ReadFileAsync(name, path);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create or update a file within a skill.
    /// </summary>
    [HttpPut("{name}/files/{**path}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<WriteFileResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WriteFile(string name, string path, [FromBody] WriteFileRequestV1 request)
    {
        var result = await _skillsService.WriteFileAsync(name, path, request);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Delete a file within a skill.
    /// </summary>
    [HttpDelete("{name}/files/{**path}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string name, string path)
    {
        var deleted = await _skillsService.DeleteFileAsync(name, path);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Rename a file or folder within a skill.
    /// </summary>
    [HttpPost("{name}/rename/{**path}")]
    [ProducesResponseType<RenameResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rename(string name, string path, [FromBody] RenameRequestV1 request)
    {
        try
        {
            var result = await _skillsService.RenameAsync(name, path, request);
            if (result is null)
                return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Duplicate a file within a skill.
    /// </summary>
    [HttpPost("{name}/duplicate/{**path}")]
    [ProducesResponseType<DuplicateFileResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DuplicateFile(string name, string path)
    {
        var result = await _skillsService.DuplicateFileAsync(name, path);
        if (result is null)
            return NotFound();
        return Created(string.Empty, result);
    }

    /// <summary>
    /// Create a folder within a skill.
    /// </summary>
    [HttpPost("{name}/folders/{**path}")]
    [ProducesResponseType<CreateFolderResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFolder(string name, string path)
    {
        try
        {
            var result = await _skillsService.CreateFolderAsync(name, path);
            if (result is null)
                return NotFound();
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a folder within a skill.
    /// </summary>
    [HttpDelete("{name}/folders/{**path}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFolder(string name, string path)
    {
        var deleted = await _skillsService.DeleteFolderAsync(name, path);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
