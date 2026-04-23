using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for ConcatAudio nodes. Looks up the upstream TTS node's output directly
/// (rather than template-rendering, since the Clips array is structured data).
/// </summary>
public class ConcatAudioNodeExecutor : NodeExecutor<ConcatAudioNodeConfiguration, ConcatAudioNodeOutput>
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<ConcatAudioNodeExecutor> _logger;

    public ConcatAudioNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        ITemplateRenderer templateRenderer,
        ILogger<ConcatAudioNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override Task<ConcatAudioNodeOutput> ExecuteInternalAsync(
        ConcatAudioNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var sourceNodeName = config.SourceNode.Trim();
        if (string.IsNullOrWhiteSpace(sourceNodeName))
        {
            throw new InvalidOperationException("ConcatAudio.SourceNode must be set to the name of an upstream TTS node.");
        }

        if (!Context.NodeOutputs.TryGetValue(sourceNodeName, out var upstream) || upstream is null)
        {
            throw new InvalidOperationException(
                $"Source node '{sourceNodeName}' has no output in this execution. Ensure it runs before this ConcatAudio node.");
        }

        if (upstream is not TextToSpeechNodeOutput ttsOutput)
        {
            throw new InvalidOperationException(
                $"Source node '{sourceNodeName}' output type '{upstream.GetType().Name}' is not a TextToSpeech output. ConcatAudio expects Clips from a TTS node.");
        }

        if (ttsOutput.Clips.Count == 0)
        {
            throw new InvalidOperationException(
                $"Source node '{sourceNodeName}' produced zero clips — nothing to concatenate.");
        }

        var format = config.Format.Trim().ToLowerInvariant();
        var clipBytes = ttsOutput.Clips
            .Select(c => Convert.FromBase64String(c.AudioBase64))
            .ToList();

        _logger.LogInformation(
            "ConcatAudio stitching {ClipCount} clips, format={Format}, source node={SourceNode}",
            clipBytes.Count, format, sourceNodeName);

        var merged = AudioConverter.Concat(clipBytes, format);
        var contentType = AudioConverter.GetContentType(format);
        var fileExtension = AudioConverter.GetFileExtension(format);
        var transcript = string.Join(" ", ttsOutput.Clips.Select(c => c.Transcript.Trim()));

        _logger.LogInformation(
            "ConcatAudio complete: {MergedBytes} bytes, {ClipCount} clips joined",
            merged.Length, clipBytes.Count);

        return Task.FromResult(new ConcatAudioNodeOutput
        {
            AudioBase64 = Convert.ToBase64String(merged),
            ContentType = contentType,
            FileExtension = fileExtension,
            SizeBytes = merged.Length,
            Transcript = transcript,
            ClipCount = ttsOutput.Clips.Count,
        });
    }
}
