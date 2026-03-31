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
/// Takes base64 audio data from config, uploads to S3, and creates a TtsRecordingEntity.
/// Metadata (content type, voice, model, transcript) is sourced from the upstream TTS output if available.
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

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Recording name is empty after template rendering");

        if (string.IsNullOrWhiteSpace(audioBase64))
            throw new InvalidOperationException("Audio data is empty after template rendering");

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "audio/mpeg";

        var ttsOutput = FindUpstreamTtsOutput();

        var fileExtension = ttsOutput?.FileExtension ?? GetExtensionFromContentType(contentType);
        var transcript = ttsOutput?.Transcript ?? string.Empty;
        var voice = ttsOutput?.Voice;
        var model = ttsOutput?.Model;

        var fileName = $"{Guid.NewGuid()}.{fileExtension}";
        var audioBytes = Convert.FromBase64String(audioBase64.Trim());
        using var audioStream = new MemoryStream(audioBytes);

        var uploadResult = await _storageService.UploadAsync(
            new UploadFileRequest
            {
                FileName = fileName,
                ContentType = contentType,
                Content = audioStream,
                KeyPrefix = $"tts/{Context.ExecutionId}"
            },
            cancellationToken);

        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name.Trim(),
            Description = (description ?? string.Empty).Trim(),
            FilePath = uploadResult.ObjectKey,
            Transcript = transcript,
            ContentType = contentType,
            SizeBytes = uploadResult.SizeBytes,
            Voice = voice,
            Model = model,
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

    private TextToSpeechNodeOutput? FindUpstreamTtsOutput()
    {
        foreach (var output in Context.NodeOutputs.Values)
        {
            if (output is TextToSpeechNodeOutput ttsOutput)
                return ttsOutput;
        }
        return null;
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
