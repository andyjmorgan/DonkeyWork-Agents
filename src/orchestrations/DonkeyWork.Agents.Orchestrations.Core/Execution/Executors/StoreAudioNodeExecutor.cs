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
/// Uploads audio bytes from the upstream TTS node to S3 and creates a TtsRecordingEntity.
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

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Recording name is empty after template rendering");
        }

        var ttsOutput = FindUpstreamTtsOutput();
        if (ttsOutput == null)
        {
            throw new InvalidOperationException(
                "No upstream TextToSpeech node output found. Connect a TTS node before StoreAudio.");
        }

        var contentType = await ResolveFieldAsync(config.AudioContentType, ttsOutput.ContentType, cancellationToken);
        var transcript = await ResolveFieldAsync(config.Transcript, ttsOutput.Transcript, cancellationToken);
        var voice = await ResolveFieldAsync(config.Voice, ttsOutput.Voice, cancellationToken);
        var model = await ResolveFieldAsync(config.Model, ttsOutput.Model, cancellationToken);

        var fileName = $"{Guid.NewGuid()}.{ttsOutput.FileExtension}";
        var audioBytes = Convert.FromBase64String(ttsOutput.AudioBase64);
        using var audioStream = new MemoryStream(audioBytes);

        var uploadResult = await _storageService.UploadAsync(
            new UploadFileRequest
            {
                FileName = fileName,
                ContentType = contentType ?? "audio/mpeg",
                Content = audioStream,
                KeyPrefix = $"tts/{Context.ExecutionId}"
            },
            cancellationToken);

        _logger.LogDebug(
            "Audio uploaded to S3: key={ObjectKey}, size={Size}",
            uploadResult.ObjectKey, uploadResult.SizeBytes);

        var objectKey = await ResolveFieldAsync(config.AudioObjectKey, uploadResult.ObjectKey, cancellationToken);

        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name.Trim(),
            Description = (description ?? string.Empty).Trim(),
            FilePath = objectKey!.Trim(),
            Transcript = transcript ?? string.Empty,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType.Trim(),
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

    private TextToSpeechNodeOutput? FindUpstreamTtsOutput()
    {
        foreach (var output in Context.NodeOutputs.Values)
        {
            if (output is TextToSpeechNodeOutput ttsOutput)
                return ttsOutput;
        }
        return null;
    }

    private async Task<string?> ResolveFieldAsync(
        string? configValue, string? autoValue, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            var rendered = await _templateRenderer.RenderAsync(configValue, cancellationToken);
            if (!string.IsNullOrWhiteSpace(rendered))
                return rendered;
        }
        return autoValue;
    }
}
