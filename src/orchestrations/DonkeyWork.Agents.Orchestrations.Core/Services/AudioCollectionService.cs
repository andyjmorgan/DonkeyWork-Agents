using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class AudioCollectionService : IAudioCollectionService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;

    public AudioCollectionService(AgentsDbContext dbContext, IIdentityContext identityContext)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
    }

    public async Task<ListAudioCollectionsResponseV1> ListAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TtsAudioCollections
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(offset)
            .Take(limit)
            .Select(c => new AudioCollectionV1
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CoverImagePath = c.CoverImagePath,
                DefaultVoice = c.DefaultVoice,
                DefaultModel = c.DefaultModel,
                RecordingCount = c.Recordings.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new ListAudioCollectionsResponseV1
        {
            Items = items,
            TotalCount = totalCount,
        };
    }

    public async Task<AudioCollectionV1?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = await _dbContext.TtsAudioCollections
            .Include(c => c.Recordings)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return collection == null ? null : Map(collection);
    }

    public async Task<AudioCollectionV1> CreateAsync(CreateAudioCollectionRequestV1 request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var collection = new TtsAudioCollectionEntity
        {
            Id = Guid.NewGuid(),
            UserId = _identityContext.UserId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            CoverImagePath = request.CoverImagePath,
            DefaultVoice = request.DefaultVoice,
            DefaultModel = request.DefaultModel,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.TtsAudioCollections.Add(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(collection);
    }

    public async Task<AudioCollectionV1?> UpdateAsync(Guid id, UpdateAudioCollectionRequestV1 request, CancellationToken cancellationToken = default)
    {
        var collection = await _dbContext.TtsAudioCollections
            .Include(c => c.Recordings)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collection == null)
        {
            return null;
        }

        if (request.Name != null)
        {
            collection.Name = request.Name;
        }

        if (request.Description != null)
        {
            collection.Description = request.Description;
        }

        if (request.CoverImagePath != null)
        {
            collection.CoverImagePath = request.CoverImagePath;
        }

        if (request.DefaultVoice != null)
        {
            collection.DefaultVoice = request.DefaultVoice;
        }

        if (request.DefaultModel != null)
        {
            collection.DefaultModel = request.DefaultModel;
        }

        collection.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(collection);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = await _dbContext.TtsAudioCollections
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (collection == null)
        {
            return false;
        }

        _dbContext.TtsAudioCollections.Remove(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ListRecordingsResponseV1?> ListRecordingsAsync(Guid id, int offset, int limit, CancellationToken cancellationToken = default)
    {
        var collectionExists = await _dbContext.TtsAudioCollections
            .AnyAsync(c => c.Id == id, cancellationToken);

        if (!collectionExists)
        {
            return null;
        }

        var query = _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .Where(r => r.CollectionId == id)
            .OrderBy(r => r.SequenceNumber)
            .ThenBy(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var recordings = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new ListRecordingsResponseV1
        {
            Items = recordings.Select(MapRecording).ToList(),
            TotalCount = totalCount,
        };
    }

    public async Task<TtsRecordingV1?> MoveRecordingAsync(Guid recordingId, MoveRecordingToCollectionRequestV1 request, CancellationToken cancellationToken = default)
    {
        var recording = await _dbContext.TtsRecordings
            .Include(r => r.Playback)
            .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

        if (recording == null)
        {
            return null;
        }

        if (request.CollectionId.HasValue)
        {
            var targetExists = await _dbContext.TtsAudioCollections
                .AnyAsync(c => c.Id == request.CollectionId.Value, cancellationToken);

            if (!targetExists)
            {
                return null;
            }
        }

        recording.CollectionId = request.CollectionId;

        if (request.SequenceNumber.HasValue)
        {
            recording.SequenceNumber = request.SequenceNumber.Value;
        }
        else if (request.CollectionId.HasValue)
        {
            var maxSequence = await _dbContext.TtsRecordings
                .Where(r => r.CollectionId == request.CollectionId.Value)
                .MaxAsync(r => (int?)r.SequenceNumber, cancellationToken) ?? 0;
            recording.SequenceNumber = maxSequence + 1;
        }
        else
        {
            recording.SequenceNumber = null;
        }

        if (request.ChapterTitle != null)
        {
            recording.ChapterTitle = request.ChapterTitle;
        }

        recording.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapRecording(recording);
    }

    private static AudioCollectionV1 Map(TtsAudioCollectionEntity entity)
    {
        return new AudioCollectionV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            CoverImagePath = entity.CoverImagePath,
            DefaultVoice = entity.DefaultVoice,
            DefaultModel = entity.DefaultModel,
            RecordingCount = entity.Recordings?.Count ?? 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
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
            CollectionId = entity.CollectionId,
            SequenceNumber = entity.SequenceNumber,
            ChapterTitle = entity.ChapterTitle,
            Status = entity.Status.ToString(),
            Progress = entity.Progress,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt,
            Playback = entity.Playback != null
                ? new TtsPlaybackV1
                {
                    PositionSeconds = entity.Playback.PositionSeconds,
                    DurationSeconds = entity.Playback.DurationSeconds,
                    Completed = entity.Playback.Completed,
                    PlaybackSpeed = entity.Playback.PlaybackSpeed,
                    UpdatedAt = entity.Playback.UpdatedAt ?? entity.Playback.CreatedAt,
                }
                : null,
        };
    }
}
