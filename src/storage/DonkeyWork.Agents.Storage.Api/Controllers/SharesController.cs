using System.Xml;
using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FileShareModel = DonkeyWork.Agents.Storage.Contracts.Models.FileShare;

namespace DonkeyWork.Agents.Storage.Api.Controllers;

/// <summary>
/// Manage file sharing operations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/shares")]
[Produces("application/json")]
public class SharesController : ControllerBase
{
    private readonly IFileShareService _shareService;

    public SharesController(IFileShareService shareService)
    {
        _shareService = shareService;
    }

    /// <summary>
    /// Create a share link for a file.
    /// </summary>
    /// <param name="request">The share request.</param>
    /// <response code="201">Returns the created share.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="404">File not found.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType<FileShareResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateShareRequestV1 request)
    {
        TimeSpan? expiresIn = null;
        if (!string.IsNullOrEmpty(request.ExpiresIn))
        {
            try
            {
                expiresIn = XmlConvert.ToTimeSpan(request.ExpiresIn);
            }
            catch (FormatException)
            {
                return BadRequest("Invalid ExpiresIn format. Use ISO 8601 duration (e.g., 'P1D' for 1 day, 'PT1H' for 1 hour).");
            }
        }

        var createRequest = new CreateShareRequest
        {
            FileId = request.FileId,
            ExpiresIn = expiresIn,
            MaxDownloads = request.MaxDownloads,
            Password = request.Password
        };

        try
        {
            var share = await _shareService.CreateShareAsync(createRequest);
            var response = ToResponse(share);
            return CreatedAtAction(nameof(GetByToken), new { token = share.ShareToken }, response);
        }
        catch (InvalidOperationException)
        {
            return NotFound("File not found or not accessible");
        }
    }

    /// <summary>
    /// Get share information by token.
    /// </summary>
    /// <param name="token">The share token.</param>
    /// <response code="200">Returns the share information.</response>
    /// <response code="404">Share not found.</response>
    [HttpGet("{token}")]
    [AllowAnonymous]
    [ProducesResponseType<FileShareResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByToken(string token)
    {
        var share = await _shareService.GetByTokenAsync(token);

        if (share == null)
            return NotFound();

        return Ok(ToResponse(share));
    }

    /// <summary>
    /// Download a file via share token.
    /// </summary>
    /// <param name="token">The share token.</param>
    /// <param name="request">Optional password for protected shares.</param>
    /// <response code="200">Returns the file content.</response>
    /// <response code="401">Invalid or missing password.</response>
    /// <response code="404">Share not found or expired.</response>
    [HttpPost("{token}/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string token, [FromBody] ShareDownloadRequestV1? request)
    {
        var result = await _shareService.DownloadByTokenAsync(token, request?.Password);

        if (result == null)
        {
            // Check if share exists to differentiate between not found and unauthorized
            var share = await _shareService.GetByTokenAsync(token);
            if (share != null && share.HasPassword)
                return Unauthorized("Invalid or missing password");

            return NotFound();
        }

        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// List all shares for a file.
    /// </summary>
    /// <param name="fileId">The file ID.</param>
    /// <param name="pagination">Pagination parameters.</param>
    /// <response code="200">Returns the list of shares.</response>
    [HttpGet("file/{fileId:guid}")]
    [Authorize]
    [ProducesResponseType<PaginatedResponse<FileShareResponseV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByFile(Guid fileId, [FromQuery] PaginationRequest pagination)
    {
        var limit = pagination.GetLimit();
        var (shares, totalCount) = await _shareService.ListByFileAsync(fileId, pagination.Offset, limit);

        var response = new PaginatedResponse<FileShareResponseV1>
        {
            Items = shares.Select(ToResponse).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Revoke a share.
    /// </summary>
    /// <param name="id">The share ID.</param>
    /// <response code="204">Share revoked.</response>
    /// <response code="404">Share not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var revoked = await _shareService.RevokeAsync(id);

        if (!revoked)
            return NotFound();

        return NoContent();
    }

    private FileShareResponseV1 ToResponse(FileShareModel share)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return new FileShareResponseV1
        {
            Id = share.Id,
            FileId = share.FileId,
            ShareToken = share.ShareToken,
            ShareUrl = $"{baseUrl}/api/v1/shares/{share.ShareToken}/download",
            ExpiresAt = share.ExpiresAt,
            Status = share.Status,
            MaxDownloads = share.MaxDownloads,
            DownloadCount = share.DownloadCount,
            HasPassword = share.HasPassword,
            CreatedAt = share.CreatedAt
        };
    }
}
