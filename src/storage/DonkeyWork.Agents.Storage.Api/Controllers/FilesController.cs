using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
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
    /// List all files for the current user.
    /// </summary>
    /// <response code="200">Returns the list of files.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<StoredFileItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination)
    {
        var limit = pagination.GetLimit();
        var (files, totalCount) = await _storageService.ListAsync(pagination.Offset, limit);

        var response = new PaginatedResponse<StoredFileItemV1>
        {
            Items = files.Select(f => new StoredFileItemV1
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                Status = f.Status,
                CreatedAt = f.CreatedAt
            }).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get file metadata by ID.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <response code="200">Returns the file metadata.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<StoredFileResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var file = await _storageService.GetByIdAsync(id);

        if (file == null)
            return NotFound();

        var response = new StoredFileResponseV1
        {
            Id = file.Id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            ChecksumSha256 = file.ChecksumSha256,
            Status = file.Status,
            CreatedAt = file.CreatedAt,
            MarkedForDeletionAt = file.MarkedForDeletionAt,
            Metadata = file.Metadata
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a presigned URL for direct file access.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <param name="expiryMinutes">Optional URL expiry in minutes (default: 60).</param>
    /// <response code="200">Returns the presigned URL.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{id:guid}/url")]
    [ProducesResponseType<GetPublicUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicUrl(Guid id, [FromQuery] int? expiryMinutes = null)
    {
        var expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : (TimeSpan?)null;
        var result = await _storageService.GetPublicUrlAsync(id, expiry);

        if (result == null)
            return NotFound();

        var response = new GetPublicUrlResponseV1
        {
            Url = result.Url,
            ExpiresAt = result.ExpiresAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a presigned URL for an image preview with optional resizing.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <param name="width">Optional width for resizing.</param>
    /// <param name="height">Optional height for resizing.</param>
    /// <param name="expiryMinutes">Optional URL expiry in minutes (default: 60).</param>
    /// <response code="200">Returns the preview URL.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType<GetPreviewUrlResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreviewUrl(Guid id, [FromQuery] int? width = null, [FromQuery] int? height = null, [FromQuery] int? expiryMinutes = null)
    {
        var expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : (TimeSpan?)null;
        var result = await _storageService.GetPreviewUrlAsync(id, width, height, expiry);

        if (result == null)
            return NotFound();

        var response = new GetPreviewUrlResponseV1
        {
            Url = result.Url,
            ExpiresAt = result.ExpiresAt,
            Width = width,
            Height = height
        };

        return Ok(response);
    }

    /// <summary>
    /// Download a file.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <response code="200">Returns the file content.</response>
    /// <response code="404">File not found.</response>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _storageService.DownloadAsync(id);

        if (result == null)
            return NotFound();

        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Upload a new file.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <response code="201">Returns the uploaded file metadata.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<StoredFileResponseV1>(StatusCodes.Status201Created)]
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

        var storedFile = await _storageService.UploadAsync(request);

        var response = new StoredFileResponseV1
        {
            Id = storedFile.Id,
            FileName = storedFile.FileName,
            ContentType = storedFile.ContentType,
            SizeBytes = storedFile.SizeBytes,
            ChecksumSha256 = storedFile.ChecksumSha256,
            Status = storedFile.Status,
            CreatedAt = storedFile.CreatedAt,
            MarkedForDeletionAt = storedFile.MarkedForDeletionAt,
            Metadata = storedFile.Metadata
        };

        return CreatedAtAction(nameof(Get), new { id = storedFile.Id }, response);
    }

    /// <summary>
    /// Delete a file (soft delete).
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <response code="204">File deleted.</response>
    /// <response code="404">File not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _storageService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
