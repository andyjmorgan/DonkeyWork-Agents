using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

public class TextToSpeechNodeExecutor : NodeExecutor<TextToSpeechNodeConfiguration, TextToSpeechNodeOutput>
{
    private readonly IExternalApiKeyService _credentialService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<TextToSpeechNodeExecutor> _logger;

    public TextToSpeechNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IExternalApiKeyService credentialService,
        ITemplateRenderer templateRenderer,
        ILogger<TextToSpeechNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _credentialService = credentialService;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<TextToSpeechNodeOutput> ExecuteInternalAsync(
        TextToSpeechNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var renderedInputs = await _templateRenderer.RenderAsync(config.Inputs, cancellationToken);
        var chunks = TtsInputParser.Parse(renderedInputs);

        var instructions = !string.IsNullOrWhiteSpace(config.Instructions)
            ? await _templateRenderer.RenderAsync(config.Instructions, cancellationToken)
            : null;

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId, config.CredentialId, cancellationToken);

        if (credential == null)
        {
            throw new InvalidOperationException($"Credential not found: {config.CredentialId}");
        }

        var apiKey = credential.Fields[CredentialFieldType.ApiKey];
        var voice = new GeneratedSpeechVoice(config.Voice);
        var format = MapResponseFormat(config.ResponseFormat);
        var contentType = GetContentType(config.ResponseFormat);
        var audioClient = new AudioClient(config.Model, apiKey);

        _logger.LogInformation(
            "OpenAI TTS fan-out: chunks={ChunkCount}, maxParallelism={MaxParallelism}, voice={Voice}, model={Model}",
            chunks.Count, config.MaxParallelism, config.Voice, config.Model);

        var clips = new AudioClip[chunks.Count];
        var parallelism = Math.Max(1, Math.Min(config.MaxParallelism, chunks.Count));

        await Parallel.ForEachAsync(
            chunks.Select((text, index) => (text, index)),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelism,
                CancellationToken = cancellationToken,
            },
            async (pair, ct) =>
            {
                var options = new SpeechGenerationOptions
                {
                    SpeedRatio = (float)config.Speed,
                    ResponseFormat = format,
                };

                if (!string.IsNullOrWhiteSpace(instructions))
                {
                    options.Instructions = instructions;
                }

                var result = await audioClient.GenerateSpeechAsync(pair.text, voice, options, ct);
                var bytes = result.Value.ToMemory().ToArray();

                clips[pair.index] = new AudioClip
                {
                    AudioBase64 = Convert.ToBase64String(bytes),
                    ContentType = contentType,
                    FileExtension = config.ResponseFormat,
                    SizeBytes = bytes.Length,
                    Transcript = pair.text,
                };
            });

        _logger.LogInformation(
            "OpenAI TTS generation complete: {ClipCount} clips, {TotalBytes} total bytes",
            clips.Length, clips.Sum(c => c.SizeBytes));

        return new TextToSpeechNodeOutput
        {
            Clips = clips,
            Voice = config.Voice,
            Model = config.Model,
        };
    }

    private static GeneratedSpeechFormat MapResponseFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => GeneratedSpeechFormat.Mp3,
            "opus" => GeneratedSpeechFormat.Opus,
            "aac" => GeneratedSpeechFormat.Aac,
            "flac" => GeneratedSpeechFormat.Flac,
            "wav" => GeneratedSpeechFormat.Wav,
            _ => GeneratedSpeechFormat.Mp3,
        };
    }

    private static string GetContentType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            _ => "audio/mpeg",
        };
    }
}
