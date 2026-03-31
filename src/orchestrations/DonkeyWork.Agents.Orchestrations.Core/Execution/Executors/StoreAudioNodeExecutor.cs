using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
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
