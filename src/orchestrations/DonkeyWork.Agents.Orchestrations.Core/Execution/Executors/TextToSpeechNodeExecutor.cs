using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

/// <summary>
/// Executor for TextToSpeech nodes.
/// Calls OpenAI TTS API to generate speech audio, then uploads the result to S3 storage.
/// </summary>
public class TextToSpeechNodeExecutor : NodeExecutor<TextToSpeechNodeConfiguration, TextToSpeechNodeOutput>
{
    private readonly IExternalApiKeyService _credentialService;
    private readonly IStorageService _storageService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<TextToSpeechNodeExecutor> _logger;

    public TextToSpeechNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IExternalApiKeyService credentialService,
        IStorageService storageService,
        ITemplateRenderer templateRenderer,
        ILogger<TextToSpeechNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _credentialService = credentialService;
        _storageService = storageService;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<TextToSpeechNodeOutput> ExecuteInternalAsync(
        TextToSpeechNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var inputText = await _templateRenderer.RenderAsync(config.InputText, cancellationToken);

        if (string.IsNullOrWhiteSpace(inputText))
        {
            throw new InvalidOperationException("Input text is empty after template rendering");
        }

        var instructions = !string.IsNullOrWhiteSpace(config.Instructions)
            ? await _templateRenderer.RenderAsync(config.Instructions, cancellationToken)
            : null;

        _logger.LogDebug(
            "TTS generation: model={Model}, voice={Voice}, text length={TextLength}",
            config.Model, config.Voice, inputText.Length);

        var credential = await _credentialService.GetByIdAsync(
            Context.UserId,
            config.CredentialId,
            cancellationToken);

        if (credential == null)
        {
            throw new InvalidOperationException($"Credential not found: {config.CredentialId}");
        }

        var apiKey = credential.Fields[CredentialFieldType.ApiKey];

        var voice = new GeneratedSpeechVoice(config.Voice);
        var format = MapResponseFormat(config.ResponseFormat);

        var audioClient = new AudioClient(config.Model, apiKey);

        var options = new SpeechGenerationOptions
        {
            SpeedRatio = (float)config.Speed,
            ResponseFormat = format
        };

        if (!string.IsNullOrWhiteSpace(instructions))
        {
            options.Instructions = instructions;
        }

        var result = await audioClient.GenerateSpeechAsync(
            inputText,
            voice,
            options,
            cancellationToken);

        var audioData = result.Value;

        _logger.LogDebug("TTS audio generated: {Size} bytes", audioData.ToMemory().Length);

        var fileExtension = config.ResponseFormat;
        var fileName = $"{Guid.NewGuid()}.{fileExtension}";
        var contentType = GetContentType(config.ResponseFormat);

        using var audioStream = audioData.ToStream();

        var uploadResult = await _storageService.UploadAsync(
            new UploadFileRequest
            {
                FileName = fileName,
                ContentType = contentType,
                Content = audioStream,
                KeyPrefix = $"tts/{Context.ExecutionId}"
            },
            cancellationToken);

        _logger.LogInformation(
            "TTS audio uploaded: key={ObjectKey}, size={Size}",
            uploadResult.ObjectKey, uploadResult.SizeBytes);

        return new TextToSpeechNodeOutput
        {
            ObjectKey = uploadResult.ObjectKey,
            FileName = uploadResult.FileName,
            ContentType = contentType,
            SizeBytes = uploadResult.SizeBytes,
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
