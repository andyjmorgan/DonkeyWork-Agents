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
        if (!string.IsNullOrWhiteSpace(collectionIdRendered) && Guid.TryParse(collectionIdRendered.Trim(), out var parsedCollectionId))
        {
            collectionId = parsedCollectionId;
        }
        else if (!string.IsNullOrWhiteSpace(collectionIdRendered))
        {
            throw new InvalidOperationException($"CollectionId '{collectionIdRendered}' is not a valid UUID.");
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
            var collectionExists = await _dbContext.TtsAudioCollections
                .AnyAsync(c => c.Id == collectionId.Value, cancellationToken);

            if (!collectionExists)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found for user.");
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
