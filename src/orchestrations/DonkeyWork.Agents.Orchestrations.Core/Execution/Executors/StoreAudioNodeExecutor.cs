using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for StoreAudio nodes.
/// Converges TTS output and model metadata to create a TtsRecordingEntity.
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
        var objectKey = await _templateRenderer.RenderAsync(config.AudioObjectKey, cancellationToken);
        var transcript = await _templateRenderer.RenderAsync(config.Transcript, cancellationToken);
        var contentType = await _templateRenderer.RenderAsync(config.AudioContentType, cancellationToken);
        var sizeBytesStr = await _templateRenderer.RenderAsync(config.AudioSizeBytes, cancellationToken);
        var voice = config.Voice != null ? await _templateRenderer.RenderAsync(config.Voice, cancellationToken) : null;
        var model = config.Model != null ? await _templateRenderer.RenderAsync(config.Model, cancellationToken) : null;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Recording name is empty after template rendering");
        }

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new InvalidOperationException("Audio object key is empty after template rendering");
        }

        long.TryParse(sizeBytesStr.Trim(), out var sizeBytes);

        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = Context.UserId,
            Name = name.Trim(),
            Description = description.Trim(),
            FilePath = objectKey.Trim(),
            Transcript = transcript,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType.Trim(),
            SizeBytes = sizeBytes,
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
}
