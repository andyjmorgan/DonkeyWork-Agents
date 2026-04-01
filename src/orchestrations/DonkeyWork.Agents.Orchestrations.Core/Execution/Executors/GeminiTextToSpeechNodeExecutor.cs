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
    private readonly ILogger<GeminiTextToSpeechNodeExecutor> _logger;

    public GeminiTextToSpeechNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IExternalApiKeyService credentialService,
        ITemplateRenderer templateRenderer,
        ILogger<GeminiTextToSpeechNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _credentialService = credentialService;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<TextToSpeechNodeOutput> ExecuteInternalAsync(
        GeminiTextToSpeechNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var inputText = await _templateRenderer.RenderAsync(config.InputText, cancellationToken);

        if (string.IsNullOrWhiteSpace(inputText))
            throw new InvalidOperationException("Input text is empty after template rendering");

        var instructions = !string.IsNullOrWhiteSpace(config.Instructions)
            ? await _templateRenderer.RenderAsync(config.Instructions, cancellationToken)
            : null;

        _logger.LogDebug(
            "Gemini TTS generation: model={Model}, voice={Voice}, text length={TextLength}",
            config.Model, config.Voice, inputText.Length);

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId, config.CredentialId, cancellationToken);

        if (credential == null)
            throw new InvalidOperationException($"Credential not found: {config.CredentialId}");

        var apiKey = credential.Fields[CredentialFieldType.ApiKey];

        var googleAi = new GoogleAi(apiKey);
        var model = googleAi.CreateGenerativeModel(config.Model);

        var prompt = instructions is not null
            ? $"{instructions}\n\n{inputText}"
            : inputText;

        var request = new GenerateContentRequest
        {
            Contents =
            [
                new Content
                {
                    Role = "user",
                    Parts = [new Part { Text = prompt }]
                }
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
                            VoiceName = config.Voice
                        }
                    }
                }
            }
        };

        var response = await model.GenerateContentAsync(request, cancellationToken);

        var inlineData = response?.Candidates?.FirstOrDefault()
            ?.Content?.Parts?.FirstOrDefault()?.InlineData;

        if (inlineData?.Data == null)
            throw new InvalidOperationException("Gemini TTS returned no audio data");

        var pcmBytes = Convert.FromBase64String(inlineData.Data);
        var outputFormat = config.ResponseFormat.ToLowerInvariant();
        var audioBytes = AudioConverter.ConvertPcm(pcmBytes, outputFormat);
        var contentType = AudioConverter.GetContentType(outputFormat);
        var fileExtension = AudioConverter.GetFileExtension(outputFormat);

        _logger.LogInformation(
            "Gemini TTS audio generated: {PcmSize} bytes PCM → {OutputSize} bytes {Format}, voice={Voice}, model={Model}",
            pcmBytes.Length, audioBytes.Length, fileExtension, config.Voice, config.Model);

        return new TextToSpeechNodeOutput
        {
            AudioBase64 = Convert.ToBase64String(audioBytes),
            ContentType = contentType,
            FileExtension = fileExtension,
            SizeBytes = audioBytes.Length,
            Transcript = inputText,
            Voice = config.Voice,
            Model = config.Model
        };
    }
}
