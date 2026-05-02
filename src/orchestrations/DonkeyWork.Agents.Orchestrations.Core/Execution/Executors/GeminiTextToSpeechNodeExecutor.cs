using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

public class GeminiTextToSpeechNodeExecutor : NodeExecutor<GeminiTextToSpeechNodeConfiguration, TextToSpeechNodeOutput>
{
    private readonly IExternalApiKeyService _credentialService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ITtsChunker _chunker;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiTextToSpeechNodeExecutor> _logger;

    public GeminiTextToSpeechNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IExternalApiKeyService credentialService,
        ITemplateRenderer templateRenderer,
        ITtsChunker chunker,
        IHttpClientFactory httpClientFactory,
        ILogger<GeminiTextToSpeechNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _credentialService = credentialService;
        _templateRenderer = templateRenderer;
        _chunker = chunker;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task<TextToSpeechNodeOutput> ExecuteInternalAsync(
        GeminiTextToSpeechNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var renderedText = await _templateRenderer.RenderAsync(config.Text, cancellationToken);

        if (string.IsNullOrWhiteSpace(renderedText))
        {
            throw new InvalidOperationException("Text is empty after template rendering.");
        }

        var chunks = _chunker.Chunk(renderedText, new ChunkerOptions
        {
            TargetCharCount = config.TargetCharCount,
            MaxCharCount = config.MaxCharCount,
        });

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("Chunker produced zero chunks from the rendered text.");
        }

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

        using var httpClient = _httpClientFactory.CreateClient("gemini-tts");

        var googleAi = new GoogleAi(apiKey, client: httpClient);
        var model = googleAi.CreateGenerativeModel(config.Model);

        var outputFormat = config.ResponseFormat.ToLowerInvariant();
        var contentType = AudioConverter.GetContentType(outputFormat);
        var fileExtension = AudioConverter.GetFileExtension(outputFormat);

        _logger.LogInformation(
            "Gemini TTS fan-out: chunks={ChunkCount}, maxParallelism={MaxParallelism}, voice={Voice}, model={Model}",
            chunks.Count, config.MaxParallelism, config.Voice, config.Model);

        var clipBytes = new byte[chunks.Count][];
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
                var prompt = instructions is not null
                    ? $"{instructions}\n\nRead aloud the following text exactly as written:\n\n{pair.text}"
                    : $"Read aloud the following text exactly as written:\n\n{pair.text}";

                var request = new GenerateContentRequest
                {
                    Contents =
                    [
                        new Content
                        {
                            Role = "user",
                            Parts = [new Part { Text = prompt }],
                        },
                    ],
                    GenerationConfig = new GenerationConfig
                    {
                        ResponseModalities = [Modality.AUDIO],
                        SpeechConfig = new SpeechConfig
                        {
                            VoiceConfig = new VoiceConfig
                            {
                                PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                                {
                                    VoiceName = config.Voice,
                                },
                            },
                        },
                    },
                };

                var response = await model.GenerateContentAsync(request, ct);
                var inlineData = response?.Candidates?.FirstOrDefault()
                    ?.Content?.Parts?.FirstOrDefault()?.InlineData;

                if (inlineData?.Data == null)
                {
                    throw new InvalidOperationException(
                        $"Gemini TTS returned no audio data for chunk index {pair.index}");
                }

                var pcmBytes = Convert.FromBase64String(inlineData.Data);
                clipBytes[pair.index] = AudioConverter.ConvertPcm(pcmBytes, outputFormat);
            });

        var stitched = clipBytes.Length == 1
            ? clipBytes[0]
            : AudioConverter.Concat(clipBytes, outputFormat);
        var transcript = string.Join(" ", chunks.Select(c => c.Trim()));

        _logger.LogInformation(
            "Gemini TTS generation complete: {ChunkCount} chunks stitched to {TotalBytes} bytes",
            chunks.Count, stitched.Length);

        return new TextToSpeechNodeOutput
        {
            AudioBase64 = Convert.ToBase64String(stitched),
            ContentType = contentType,
            FileExtension = fileExtension,
            SizeBytes = stitched.Length,
            Transcript = transcript,
            Voice = config.Voice,
            Model = config.Model,
        };
    }
}
