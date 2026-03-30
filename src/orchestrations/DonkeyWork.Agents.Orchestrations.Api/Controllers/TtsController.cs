using Asp.Versioning;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Orchestrations.Api.Controllers;

/// <summary>
/// Manage TTS recordings and playback state.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tts")]
[Authorize]
[Produces("application/json")]
public class TtsController : ControllerBase
{
    private readonly AgentsDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly IIdentityContext _identityContext;

    public TtsController(
        AgentsDbContext dbContext,
        IStorageService storageService,
        IIdentityContext identityContext)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// List all TTS recordings for the current user.
    /// </summary>
    [HttpGet("recordings")]
    [ProducesResponseType<TtsRecordingListResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRecordings(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var recordings = await query
            .Skip(offset)
            .Take(limit)
            .Select(r => new TtsRecordingResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                FilePath = r.FilePath,
                Transcript = r.Transcript,
                ContentType = r.ContentType,
                SizeBytes = r.SizeBytes,
                Voice = r.Voice,
                Model = r.Model,
                CreatedAt = r.CreatedAt,
                Playback = r.Playback != null ? new TtsPlaybackResponse
                {
                    PositionSeconds = r.Playback.PositionSeconds,
                    DurationSeconds = r.Playback.DurationSeconds,
                    Completed = r.Playback.Completed,
                    PlaybackSpeed = r.Playback.PlaybackSpeed,
                    UpdatedAt = r.Playback.UpdatedAt ?? r.Playback.CreatedAt
                } : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new TtsRecordingListResponse
        {
            Items = recordings,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a specific TTS recording by ID.
    /// </summary>
    [HttpGet("recordings/{id:guid}")]
    [ProducesResponseType<TtsRecordingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecording(Guid id, CancellationToken cancellationToken)
    {
        var recording = await _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return NotFound();

        return Ok(new TtsRecordingResponse
        {
            Id = recording.Id,
            Name = recording.Name,
            Description = recording.Description,
            FilePath = recording.FilePath,
            Transcript = recording.Transcript,
            ContentType = recording.ContentType,
            SizeBytes = recording.SizeBytes,
            Voice = recording.Voice,
            Model = recording.Model,
            CreatedAt = recording.CreatedAt,
            Playback = recording.Playback != null ? new TtsPlaybackResponse
            {
                PositionSeconds = recording.Playback.PositionSeconds,
                DurationSeconds = recording.Playback.DurationSeconds,
                Completed = recording.Playback.Completed,
                PlaybackSpeed = recording.Playback.PlaybackSpeed,
                UpdatedAt = recording.Playback.UpdatedAt ?? recording.Playback.CreatedAt
            } : null
        });
    }

    /// <summary>
    /// Get a presigned URL for the audio file of a recording.
    /// </summary>
    [HttpGet("recordings/{id:guid}/audio")]
    [ProducesResponseType<TtsAudioUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudioUrl(Guid id, CancellationToken cancellationToken)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return NotFound();

        var presigned = await _storageService.GetPublicUrlAsync(
            recording.FilePath,
            TimeSpan.FromHours(1),
            cancellationToken);

        if (presigned == null)
            return NotFound("Audio file not found in storage");

        return Ok(new TtsAudioUrlResponse
        {
            Url = presigned.Url,
            ExpiresAt = presigned.ExpiresAt,
            ContentType = recording.ContentType
        });
    }

    /// <summary>
    /// Update playback state for a recording (last write wins).
    /// </summary>
    [HttpPut("recordings/{id:guid}/playback")]
    [ProducesResponseType<TtsPlaybackResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayback(
        Guid id,
        [FromBody] UpdatePlaybackRequest request,
        CancellationToken cancellationToken)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return NotFound();

        var userId = _identityContext.UserId;

        var playback = await _dbContext.TtsPlayback
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.RecordingId == id && p.UserId == userId,
                cancellationToken);

        if (playback == null)
        {
            playback = new TtsPlaybackEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RecordingId = id,
                PositionSeconds = request.PositionSeconds,
                DurationSeconds = request.DurationSeconds,
                Completed = request.Completed,
                PlaybackSpeed = request.PlaybackSpeed,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.TtsPlayback.Add(playback);
        }
        else
        {
            // Last write wins
            playback.PositionSeconds = request.PositionSeconds;
            playback.DurationSeconds = request.DurationSeconds;
            playback.Completed = request.Completed;
            playback.PlaybackSpeed = request.PlaybackSpeed;
            playback.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new TtsPlaybackResponse
        {
            PositionSeconds = playback.PositionSeconds,
            DurationSeconds = playback.DurationSeconds,
            Completed = playback.Completed,
            PlaybackSpeed = playback.PlaybackSpeed,
            UpdatedAt = playback.UpdatedAt ?? playback.CreatedAt
        });
    }

    /// <summary>
    /// Get playback state for a recording.
    /// </summary>
    [HttpGet("recordings/{id:guid}/playback")]
    [ProducesResponseType<TtsPlaybackResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayback(Guid id, CancellationToken cancellationToken)
    {
        var userId = _identityContext.UserId;

        var playback = await _dbContext.TtsPlayback
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.RecordingId == id && p.UserId == userId,
                cancellationToken);

        if (playback == null)
        {
            return Ok(new TtsPlaybackResponse
            {
                PositionSeconds = 0,
                DurationSeconds = 0,
                Completed = false,
                PlaybackSpeed = 1.0,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        return Ok(new TtsPlaybackResponse
        {
            PositionSeconds = playback.PositionSeconds,
            DurationSeconds = playback.DurationSeconds,
            Completed = playback.Completed,
            PlaybackSpeed = playback.PlaybackSpeed,
            UpdatedAt = playback.UpdatedAt ?? playback.CreatedAt
        });
    }

    /// <summary>
    /// Delete a TTS recording.
    /// </summary>
    [HttpDelete("recordings/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecording(Guid id, CancellationToken cancellationToken)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return NotFound();

        // Delete the audio file from storage
        await _storageService.DeleteAsync(recording.FilePath, cancellationToken);

        _dbContext.TtsRecordings.Remove(recording);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

// ============================================================================
// Request/Response Models
// ============================================================================

public class TtsRecordingListResponse
{
    public required List<TtsRecordingResponse> Items { get; init; }
    public required int TotalCount { get; init; }
}

public class TtsRecordingResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
    public required string Transcript { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public string? Voice { get; init; }
    public string? Model { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public TtsPlaybackResponse? Playback { get; init; }
}

public class TtsAudioUrlResponse
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string ContentType { get; init; }
}

public class TtsPlaybackResponse
{
    public required double PositionSeconds { get; init; }
    public required double DurationSeconds { get; init; }
    public required bool Completed { get; init; }
    public required double PlaybackSpeed { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public class UpdatePlaybackRequest
{
    public required double PositionSeconds { get; init; }
    public required double DurationSeconds { get; init; }
    public bool Completed { get; init; }
    public double PlaybackSpeed { get; init; } = 1.0;
}
