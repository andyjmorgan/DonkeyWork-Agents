using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Providers.Contracts.Services;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

public class TextToSpeechNodeExecutor : NodeExecutor<TextToSpeechNodeConfiguration, TextToSpeechNodeOutput>
{
    private readonly IExternalApiKeyService _credentialService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IModelCatalogService _modelCatalogService;
    private readonly ILogger<TextToSpeechNodeExecutor> _logger;

    public TextToSpeechNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IExternalApiKeyService credentialService,
        ITemplateRenderer templateRenderer,
        IModelCatalogService modelCatalogService,
        ILogger<TextToSpeechNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _credentialService = credentialService;
        _templateRenderer = templateRenderer;
        _modelCatalogService = modelCatalogService;
        _logger = logger;
    }

    protected override async Task<TextToSpeechNodeOutput> ExecuteInternalAsync(
        TextToSpeechNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var inputText = await _templateRenderer.RenderAsync(config.InputText, cancellationToken);

        if (string.IsNullOrWhiteSpace(inputText))
            throw new InvalidOperationException("Input text is empty after template rendering");

        var instructions = !string.IsNullOrWhiteSpace(config.Instructions)
            ? await _templateRenderer.RenderAsync(config.Instructions, cancellationToken)
            : null;

        _logger.LogDebug(
            "TTS generation: model={Model}, voice={Voice}, text length={TextLength}",
            config.Model, config.Voice, inputText.Length);

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId, config.CredentialId, cancellationToken);

        if (credential == null)
            throw new InvalidOperationException($"Credential not found: {config.CredentialId}");

        var apiKey = credential.Fields[CredentialFieldType.ApiKey];

        var model = _modelCatalogService.GetModelById(config.Model);
        var provider = model?.Provider ?? LlmProvider.OpenAI;

        return provider switch
        {
            LlmProvider.Google => await GenerateWithGeminiAsync(config, inputText, instructions, apiKey, cancellationToken),
            _ => await GenerateWithOpenAiAsync(config, inputText, instructions, apiKey, cancellationToken),
        };
    }

    private async Task<TextToSpeechNodeOutput> GenerateWithOpenAiAsync(
        TextToSpeechNodeConfiguration config,
        string inputText,
        string? instructions,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var voice = new GeneratedSpeechVoice(config.Voice);
        var format = MapResponseFormat(config.ResponseFormat);
        var audioClient = new AudioClient(config.Model, apiKey);

        var options = new SpeechGenerationOptions
        {
            SpeedRatio = (float)config.Speed,
            ResponseFormat = format
        };

        if (!string.IsNullOrWhiteSpace(instructions))
            options.Instructions = instructions;

        var result = await audioClient.GenerateSpeechAsync(inputText, voice, options, cancellationToken);
        var audioBytes = result.Value.ToMemory().ToArray();

        _logger.LogInformation(
            "TTS audio generated (OpenAI): {Size} bytes, voice={Voice}, model={Model}",
            audioBytes.Length, config.Voice, config.Model);

        return new TextToSpeechNodeOutput
        {
            AudioBase64 = Convert.ToBase64String(audioBytes),
            ContentType = GetContentType(config.ResponseFormat),
            FileExtension = config.ResponseFormat,
            SizeBytes = audioBytes.Length,
            Transcript = inputText,
            Voice = config.Voice,
            Model = config.Model
        };
    }

    private async Task<TextToSpeechNodeOutput> GenerateWithGeminiAsync(
        TextToSpeechNodeConfiguration config,
        string inputText,
        string? instructions,
        string apiKey,
        CancellationToken cancellationToken)
    {
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
        var mp3Bytes = AudioConverter.ConvertPcmToMp3(pcmBytes);

        _logger.LogInformation(
            "TTS audio generated (Gemini): {PcmSize} bytes PCM → {Mp3Size} bytes MP3, voice={Voice}, model={Model}",
            pcmBytes.Length, mp3Bytes.Length, config.Voice, config.Model);

        return new TextToSpeechNodeOutput
        {
            AudioBase64 = Convert.ToBase64String(mp3Bytes),
            ContentType = "audio/mpeg",
            FileExtension = "mp3",
            SizeBytes = mp3Bytes.Length,
            Transcript = inputText,
            Voice = config.Voice,
            Model = config.Model
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
            _ => GeneratedSpeechFormat.Mp3
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
            _ => "audio/mpeg"
        };
    }
}
