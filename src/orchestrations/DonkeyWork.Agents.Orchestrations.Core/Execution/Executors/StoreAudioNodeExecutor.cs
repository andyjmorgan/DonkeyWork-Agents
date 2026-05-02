using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for StoreAudio nodes.
/// All fields come from the node configuration (template-rendered).
/// No implicit upstream scanning.
/// </summary>
public class StoreAudioNodeExecutor : NodeExecutor<StoreAudioNodeConfiguration, StoreAudioNodeOutput>
{
    private readonly AgentsDbContext _dbContext;
    private readonly IStorageService _storageService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<StoreAudioNodeExecutor> _logger;

    public StoreAudioNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        AgentsDbContext dbContext,
        IStorageService storageService,
        ITemplateRenderer templateRenderer,
        ILogger<StoreAudioNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<StoreAudioNodeOutput> ExecuteInternalAsync(
        StoreAudioNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var name = await _templateRenderer.RenderAsync(config.RecordingName, cancellationToken);
        var description = await _templateRenderer.RenderAsync(config.RecordingDescription, cancellationToken);
        var audioBase64 = await _templateRenderer.RenderAsync(config.AudioBase64, cancellationToken);
        var contentType = await _templateRenderer.RenderAsync(config.ContentType, cancellationToken);
        var voice = config.Voice != null ? await _templateRenderer.RenderAsync(config.Voice, cancellationToken) : null;
        var model = config.Model != null ? await _templateRenderer.RenderAsync(config.Model, cancellationToken) : null;
        var transcript = config.Transcript != null ? await _templateRenderer.RenderAsync(config.Transcript, cancellationToken) : null;
        var fileExtension = config.FileExtension != null ? await _templateRenderer.RenderAsync(config.FileExtension, cancellationToken) : null;
        var collectionIdRendered = config.CollectionId != null ? await _templateRenderer.RenderAsync(config.CollectionId, cancellationToken) : null;
        var sequenceNumberRendered = config.SequenceNumber != null ? await _templateRenderer.RenderAsync(config.SequenceNumber, cancellationToken) : null;
        var chapterTitle = config.ChapterTitle != null ? await _templateRenderer.RenderAsync(config.ChapterTitle, cancellationToken) : null;

        Guid? collectionId = null;
        if (!string.IsNullOrWhiteSpace(collectionIdRendered))
        {
            var trimmed = collectionIdRendered.Trim();
            if (Guid.TryParse(trimmed, out var parsedCollectionId))
            {
                collectionId = parsedCollectionId;
            }
            else
            {
                collectionId = await ResolveOrCreateCollectionByNameAsync(trimmed, cancellationToken);
            }
        }

        int? sequenceNumber = null;
        if (!string.IsNullOrWhiteSpace(sequenceNumberRendered))
        {
            if (!int.TryParse(sequenceNumberRendered.Trim(), out var parsedSequence))
            {
                throw new InvalidOperationException($"SequenceNumber '{sequenceNumberRendered}' is not a valid integer.");
            }

            sequenceNumber = parsedSequence;
        }

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Recording name is empty after template rendering");

        if (string.IsNullOrWhiteSpace(audioBase64))
            throw new InvalidOperationException("Audio data is empty after template rendering");

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "audio/mpeg";

        if (string.IsNullOrWhiteSpace(fileExtension))
            fileExtension = GetExtensionFromContentType(contentType);

        var fileName = $"{Guid.NewGuid()}.{fileExtension.Trim()}";
        var audioBytes = Convert.FromBase64String(audioBase64.Trim());

        // Guard: a common misconfiguration is to wire StoreAudio directly to a
        // chunked TextToSpeech node via {{ Steps.tts.AudioBase64 }} (which proxies
        // Clips[0]). If the upstream TTS produced multiple clips and the rendered
        // payload is exactly the first clip, the orchestration is silently dropping
        // the rest of the audio. Detect that and fail loudly so the user inserts
        // a ConcatAudio node instead of shipping a truncated recording.
        foreach (var upstream in Context.NodeOutputs.Values)
        {
            if (upstream is TextToSpeechNodeOutput tts && tts.ClipCount > 1
                && tts.Clips[0].SizeBytes == audioBytes.Length)
            {
                throw new InvalidOperationException(
                    $"StoreAudio received only the first of {tts.ClipCount} TTS clips " +
                    $"({audioBytes.Length:N0} bytes vs {tts.TotalSizeBytes:N0} bytes total). " +
                    "Insert a ConcatAudio node between the TextToSpeech node and StoreAudio, " +
                    "and point StoreAudio's audio fields at the ConcatAudio output instead.");
            }
        }

        using var audioStream = new MemoryStream(audioBytes);

        var uploadResult = await _storageService.UploadAsync(
            new UploadFileRequest
            {
                FileName = fileName,
                ContentType = contentType.Trim(),
                Content = audioStream,
                KeyPrefix = $"tts/{Context.UserId}/{Context.ExecutionId}",
                AbsoluteKeyPrefix = true
            },
            cancellationToken);

        if (collectionId.HasValue)
        {
            // Enforce ownership on UUID-from-template collection refs. The DbContext
            // global query filter scopes this to the current user, so AnyAsync returns
            // false for another user's collection — prevents templated foreign UUIDs
            // from leaking recordings into someone else's folder. Auto-created
            // name-based collections were just inserted with the current user, so this
            // also passes on the create-on-the-fly path.
            var collectionExists = await _dbContext.TtsAudioCollections
                .AnyAsync(c => c.Id == collectionId.Value, cancellationToken);

            if (!collectionExists)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found.");
            }

            if (!sequenceNumber.HasValue)
            {
                var maxSeq = await _dbContext.TtsRecordings
                    .Where(r => r.CollectionId == collectionId.Value)
                    .MaxAsync(r => (int?)r.SequenceNumber, cancellationToken) ?? 0;
                sequenceNumber = maxSeq + 1;
            }
        }

        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name.Trim(),
            Description = (description ?? string.Empty).Trim(),
            FilePath = uploadResult.ObjectKey,
            Transcript = transcript ?? string.Empty,
            ContentType = contentType.Trim(),
            SizeBytes = uploadResult.SizeBytes,
            Voice = string.IsNullOrWhiteSpace(voice) ? null : voice.Trim(),
            Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim(),
            OrchestrationExecutionId = Context.ExecutionId,
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = string.IsNullOrWhiteSpace(chapterTitle) ? null : chapterTitle.Trim(),
            Status = TtsRecordingStatus.Ready,
            Progress = 1.0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.TtsRecordings.Add(recording);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TTS recording stored: id={RecordingId}, name={Name}, file={FilePath}",
            recording.Id, recording.Name, recording.FilePath);

        return new StoreAudioNodeOutput
        {
            RecordingId = recording.Id,
            Name = recording.Name,
            Description = recording.Description,
            FilePath = recording.FilePath,
            Transcript = recording.Transcript
        };
    }

    /// <summary>
    /// Looks up an existing collection by (case-insensitive) name for the current user;
    /// creates a new one with that name if none matches.
    /// </summary>
    private async Task<Guid> ResolveOrCreateCollectionByNameAsync(string name, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TtsAudioCollections
            .Where(c => c.Name.ToLower() == name.ToLower())
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing.HasValue)
        {
            return existing.Value;
        }

        var now = DateTimeOffset.UtcNow;
        var collection = new TtsAudioCollectionEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name,
            Description = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.TtsAudioCollections.Add(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-created audio collection '{Name}' ({CollectionId}) for user {UserId}",
            name, collection.Id, Context.UserId);

        return collection.Id;
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "audio/mpeg" => "mp3",
            "audio/opus" => "opus",
            "audio/aac" => "aac",
            "audio/flac" => "flac",
            "audio/wav" => "wav",
            _ => "mp3"
        };
    }
}
