using Asp.Versioning;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Storage.Api.Controllers;

/// <summary>
/// Manage file storage operations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/files")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IStorageService _storageService;

    public FilesController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// List files and folders for the current user.
    /// </summary>
    /// <param name="prefix">Optional folder prefix to list contents of a subfolder.</param>
    /// <response code="200">Returns the listing of files and folders.</response>
    [HttpGet]
    [ProducesResponseType<FileListingResponseV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? prefix = null)
    {
        var listing = await _storageService.ListAsync(prefix);
        return Ok(listing);
    }

    /// <summary>
    /// Upload a new file.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <response code="201">Returns the uploaded file details.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<StorageUploadResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("File is empty");

        var request = new UploadFileRequest
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Content = file.OpenReadStream()
        };

        var result = await _storageService.UploadAsync(request);
        return Created(string.Empty, result);
    }

    /// <summary>
    /// Download a user file by filename.
    /// </summary>
    /// <param name="filename">The filename to download.</param>
    /// <response code="200">Returns the file content.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{filename}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string filename)
    {
        var result = await _storageService.DownloadAsync(filename);
        if (result == null)
            return NotFound();

        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Get a presigned URL for a user file.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <param name="expiryMinutes">Optional URL expiry in minutes (default: 60).</param>
    /// <response code="200">Returns the presigned URL.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{filename}/url")]
    [ProducesResponseType<GetPublicUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicUrl(string filename, [FromQuery] int? expiryMinutes = null)
    {
        var expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : (TimeSpan?)null;
        var result = await _storageService.GetPublicUrlAsync(filename, expiry);

        if (result == null)
            return NotFound();

        return Ok(new GetPublicUrlResponseV1
        {
            Url = result.Url,
            ExpiresAt = result.ExpiresAt
        });
    }

    /// <summary>
    /// Delete a user file by filename.
    /// </summary>
    /// <param name="filename">The filename to delete.</param>
    /// <response code="204">File deleted.</response>
    /// <response code="404">File not found.</response>
    [HttpDelete("{filename}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string filename)
    {
        var deleted = await _storageService.DeleteAsync(filename);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Delete a folder and all its contents.
    /// </summary>
    /// <param name="prefix">The folder path to delete (relative to user root).</param>
    /// <response code="204">Folder deleted.</response>
    [HttpDelete("folder/{**prefix}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFolder(string prefix)
    {
        await _storageService.DeleteFolderAsync(prefix);
        return NoContent();
    }

    /// <summary>
    /// Download a file by full path key (e.g., conversations/{convId}/image.png).
    /// </summary>
    /// <param name="key">The object key path relative to user namespace.</param>
    /// <response code="200">Returns the file content.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("download/{**key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByKey(string key)
    {
        var result = await _storageService.DownloadAsync(key);
        if (result == null)
            return NotFound();

        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Get a presigned URL for a file by full path key.
    /// </summary>
    /// <param name="key">The object key path relative to user namespace.</param>
    /// <param name="expiryMinutes">Optional URL expiry in minutes (default: 60).</param>
    /// <response code="200">Returns the presigned URL.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("url/{**key}")]
    [ProducesResponseType<GetPublicUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicUrlByKey(string key, [FromQuery] int? expiryMinutes = null)
    {
        var expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : (TimeSpan?)null;
        var result = await _storageService.GetPublicUrlAsync(key, expiry);

        if (result == null)
            return NotFound();

        return Ok(new GetPublicUrlResponseV1
        {
            Url = result.Url,
            ExpiresAt = result.ExpiresAt
        });
    }
}
