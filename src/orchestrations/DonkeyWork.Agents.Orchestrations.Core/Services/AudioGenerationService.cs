using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Messages;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class AudioGenerationService : IAudioGenerationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<AudioGenerationService> _logger;

    public AudioGenerationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IMessageBus messageBus,
        ILogger<AudioGenerationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _messageBus = messageBus;
        _logger = logger;
    }

    public async Task<Guid> StartGenerationAsync(StartAudioGenerationRequestV1 request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Text must not be empty.", nameof(request));
        }

        if (request.MaxCharCount < request.TargetCharCount)
        {
            throw new ArgumentException(
                $"MaxCharCount ({request.MaxCharCount}) must be >= TargetCharCount ({request.TargetCharCount}).",
                nameof(request));
        }

        var userId = _identityContext.UserId;

        int? resolvedSequence = request.SequenceNumber;
        if (request.CollectionId.HasValue)
        {
            var collectionExists = await _dbContext.TtsAudioCollections
                .AnyAsync(c => c.Id == request.CollectionId.Value, cancellationToken);

            if (!collectionExists)
            {
                throw new InvalidOperationException($"Collection {request.CollectionId} not found for this user.");
            }

            if (!resolvedSequence.HasValue)
            {
                var maxSeq = await _dbContext.TtsRecordings
                    .Where(r => r.CollectionId == request.CollectionId.Value)
                    .MaxAsync(r => (int?)r.SequenceNumber, cancellationToken) ?? 0;
                resolvedSequence = maxSeq + 1;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            FilePath = string.Empty,
            Transcript = request.Text,
            ContentType = MapContentType(request.ResponseFormat),
            SizeBytes = 0,
            Voice = request.Voice,
            Model = request.Model,
            CollectionId = request.CollectionId,
            SequenceNumber = resolvedSequence,
            ChapterTitle = string.IsNullOrWhiteSpace(request.ChapterTitle) ? null : request.ChapterTitle.Trim(),
            Status = TtsRecordingStatus.Pending,
            Progress = 0.0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.TtsRecordings.Add(recording);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var command = new GenerateAudioRecordingCommand(
            recording.Id,
            userId,
            request.Text,
            request.Model,
            request.Voice,
            request.Instructions,
            request.TargetCharCount,
            request.MaxCharCount,
            request.MaxParallelism,
            request.ResponseFormat,
            request.Speed);

        await _messageBus.PublishAsync(command);

        _logger.LogInformation(
            "Enqueued audio generation for recording {RecordingId} (user {UserId}, model {Model}, collection {CollectionId})",
            recording.Id, userId, request.Model, request.CollectionId);

        return recording.Id;
    }

    private static string MapContentType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            _ => "audio/mpeg",
        };
    }
}
