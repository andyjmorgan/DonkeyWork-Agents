using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for StoreAudio nodes.
/// Persists a TTS recording entity. Audio metadata auto-resolves from the upstream
/// TextToSpeech node output unless explicitly overridden in the configuration.
/// </summary>
public class StoreAudioNodeExecutor : NodeExecutor<StoreAudioNodeConfiguration, StoreAudioNodeOutput>
{
    private readonly AgentsDbContext _dbContext;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<StoreAudioNodeExecutor> _logger;

    public StoreAudioNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        AgentsDbContext dbContext,
        ITemplateRenderer templateRenderer,
        ILogger<StoreAudioNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _dbContext = dbContext;
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

        var objectKey = await ResolveFieldAsync(config.AudioObjectKey, ttsOutput?.ObjectKey, cancellationToken);
        var transcript = await ResolveFieldAsync(config.Transcript, ttsOutput?.Transcript, cancellationToken);
        var contentType = await ResolveFieldAsync(config.AudioContentType, ttsOutput?.ContentType, cancellationToken);
        var voice = await ResolveFieldAsync(config.Voice, ttsOutput?.Voice, cancellationToken);
        var model = await ResolveFieldAsync(config.Model, ttsOutput?.Model, cancellationToken);

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new InvalidOperationException(
                "Audio object key could not be resolved. Either connect a TextToSpeech node upstream or set the field manually.");
        }

        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name.Trim(),
            Description = (description ?? string.Empty).Trim(),
            FilePath = objectKey.Trim(),
            Transcript = transcript ?? string.Empty,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType.Trim(),
            SizeBytes = ttsOutput?.SizeBytes ?? 0,
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
