using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class TtsService : ITtsService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly IIdentityContext _identityContext;

    public TtsService(
        AgentsDbContext dbContext,
        IStorageService storageService,
        IIdentityContext identityContext)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _identityContext = identityContext;
    }

    public async Task<ListRecordingsResponseV1> ListRecordingsAsync(
        int offset, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var recordings = await query
            .Skip(offset)
            .Take(limit)
            .Select(r => MapRecording(r))
            .ToListAsync(cancellationToken);

        return new ListRecordingsResponseV1
        {
            Items = recordings,
            TotalCount = totalCount
        };
    }

    public async Task<TtsRecordingV1?> GetRecordingAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var recording = await _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return recording == null ? null : MapRecording(recording);
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> DownloadAudioAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return null;

        var download = await _storageService.DownloadAsync(recording.FilePath, cancellationToken);
        if (download == null)
            return null;

        return (download.Content, recording.ContentType, download.FileName);
    }

    public async Task<TtsPlaybackV1> GetPlaybackAsync(
        Guid recordingId, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;

        var playback = await _dbContext.TtsPlayback
            .FirstOrDefaultAsync(
                p => p.RecordingId == recordingId,
                cancellationToken);

        if (playback == null)
        {
            return new TtsPlaybackV1
            {
                PositionSeconds = 0,
                DurationSeconds = 0,
                Completed = false,
                PlaybackSpeed = 1.0,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        return MapPlayback(playback);
    }

    public async Task<TtsPlaybackV1?> UpdatePlaybackAsync(
        Guid recordingId, UpdatePlaybackRequestV1 request, CancellationToken cancellationToken = default)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

        if (recording == null)
            return null;

        var userId = _identityContext.UserId;

        var playback = await _dbContext.TtsPlayback
            .FirstOrDefaultAsync(
                p => p.RecordingId == recordingId,
                cancellationToken);

        if (playback == null)
        {
            playback = new TtsPlaybackEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RecordingId = recordingId,
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
            playback.PositionSeconds = request.PositionSeconds;
            playback.DurationSeconds = request.DurationSeconds;
            playback.Completed = request.Completed;
            playback.PlaybackSpeed = request.PlaybackSpeed;
            playback.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapPlayback(playback);
    }

    public async Task<bool> DeleteRecordingAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var recording = await _dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recording == null)
            return false;

        await _storageService.DeleteAsync(recording.FilePath, cancellationToken);

        _dbContext.TtsRecordings.Remove(recording);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static TtsRecordingV1 MapRecording(TtsRecordingEntity entity)
    {
        return new TtsRecordingV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            FilePath = entity.FilePath,
            Transcript = entity.Transcript,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            Voice = entity.Voice,
            Model = entity.Model,
            CreatedAt = entity.CreatedAt,
            Playback = entity.Playback != null ? MapPlayback(entity.Playback) : null
        };
    }

    private static TtsPlaybackV1 MapPlayback(TtsPlaybackEntity entity)
    {
        return new TtsPlaybackV1
        {
            PositionSeconds = entity.PositionSeconds,
            DurationSeconds = entity.DurationSeconds,
            Completed = entity.Completed,
            PlaybackSpeed = entity.PlaybackSpeed,
            UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt
        };
    }
}
