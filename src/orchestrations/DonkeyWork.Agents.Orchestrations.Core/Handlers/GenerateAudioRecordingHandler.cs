using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Messages;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using DonkeyWork.Agents.Storage.Contracts.Services;
using GenerativeAI;
using GenerativeAI.Types;
using StorageModels = DonkeyWork.Agents.Storage.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace DonkeyWork.Agents.Orchestrations.Core.Handlers;

/// <summary>
/// Background handler for agent-initiated audio recordings. Runs the
/// chunk → TTS → concat → upload → store pipeline and updates the recording's
/// Status and Progress fields as it goes.
/// </summary>
public static class GenerateAudioRecordingHandler
{
    public static async Task Handle(
        GenerateAudioRecordingCommand command,
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ITtsChunker chunker,
        IExternalApiKeyService credentialService,
        IStorageService storageService,
        INotificationService notificationService,
        ILogger<GenerateAudioRecordingCommand> logger,
        CancellationToken cancellationToken)
    {
        identityContext.SetIdentity(command.UserId);

        var recording = await dbContext.TtsRecordings
            .FirstOrDefaultAsync(r => r.Id == command.RecordingId, cancellationToken);

        if (recording == null)
        {
            logger.LogError("Recording {RecordingId} not found; cannot generate", command.RecordingId);
            return;
        }

        try
        {
            recording.Status = TtsRecordingStatus.Generating;
            recording.Progress = 0.0;
            recording.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyAsync(notificationService, command.UserId, recording, "Generating audio", $"{recording.Name} is generating…", cancellationToken);

            var chunks = chunker.Chunk(command.Text, new ChunkerOptions
            {
                TargetCharCount = command.TargetCharCount,
                MaxCharCount = command.MaxCharCount,
            });

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("Chunker produced zero chunks from the input text.");
            }

            logger.LogInformation(
                "Generating audio for recording {RecordingId}: {ChunkCount} chunks, model={Model}",
                recording.Id, chunks.Count, command.Model);

            var provider = ResolveProvider(command.Model);
            var apiKey = await ResolveApiKeyAsync(credentialService, command.UserId, provider, cancellationToken);
            var outputFormat = command.ResponseFormat.ToLowerInvariant();

            var clipBytes = provider == ExternalApiKeyProvider.Google
                ? await GenerateWithGeminiAsync(chunks, command, apiKey, outputFormat, cancellationToken)
                : await GenerateWithOpenAiAsync(chunks, command, apiKey, outputFormat, cancellationToken);

            var stitched = clipBytes.Length == 1
                ? clipBytes[0]
                : AudioConverter.Concat(clipBytes, outputFormat);

            var contentType = AudioConverter.GetContentType(outputFormat);
            var fileExtension = AudioConverter.GetFileExtension(outputFormat);
            var fileName = $"{Guid.NewGuid()}.{fileExtension}";

            using var audioStream = new MemoryStream(stitched);
            var uploadResult = await storageService.UploadAsync(
                new StorageModels.UploadFileRequest
                {
                    FileName = fileName,
                    ContentType = contentType,
                    Content = audioStream,
                    KeyPrefix = $"tts/{command.UserId}/agent/{recording.Id}",
                    AbsoluteKeyPrefix = true,
                },
                cancellationToken);

            recording.FilePath = uploadResult.ObjectKey;
            recording.ContentType = contentType;
            recording.SizeBytes = uploadResult.SizeBytes;
            recording.Status = TtsRecordingStatus.Ready;
            recording.Progress = 1.0;
            recording.ErrorMessage = null;
            recording.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyAsync(notificationService, command.UserId, recording, "Audio ready", $"{recording.Name} is ready to play.", cancellationToken);

            logger.LogInformation(
                "Recording {RecordingId} ready: {SizeBytes} bytes, {ClipCount} clips stitched",
                recording.Id, uploadResult.SizeBytes, chunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audio generation failed for recording {RecordingId}", command.RecordingId);

            try
            {
                recording.Status = TtsRecordingStatus.Failed;
                recording.Progress = 0.0;
                recording.ErrorMessage = ex.Message;
                recording.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(CancellationToken.None);
                await NotifyAsync(notificationService, command.UserId, recording, "Audio generation failed", recording.Name + ": " + ex.Message, CancellationToken.None);
            }
            catch (Exception persistEx)
            {
                logger.LogCritical(persistEx,
                    "Failed to persist Failed status for recording {RecordingId}", command.RecordingId);
            }
        }
    }

    private static async Task NotifyAsync(
        INotificationService notificationService,
        Guid userId,
        TtsRecordingEntity recording,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.SendToUserAsync(
                userId,
                new WorkspaceNotification
                {
                    Type = NotificationType.AudioRecordingUpdated,
                    Title = title,
                    Message = message,
                    EntityId = recording.Id,
                    ParentId = recording.CollectionId,
                },
                cancellationToken);
        }
        catch
        {
            // Non-fatal — notification failures must not sink the generation.
        }
    }

    private static ExternalApiKeyProvider ResolveProvider(string model)
    {
        if (model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase))
        {
            return ExternalApiKeyProvider.Google;
        }

        return ExternalApiKeyProvider.OpenAI;
    }

    private static async Task<string> ResolveApiKeyAsync(
        IExternalApiKeyService credentialService,
        Guid userId,
        ExternalApiKeyProvider provider,
        CancellationToken cancellationToken)
    {
        var keys = await credentialService.GetByProviderAsync(userId, provider, cancellationToken);
        var first = keys.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No {provider} credential configured for this user. Add one in Credentials before generating audio.");

        return first.Fields[CredentialFieldType.ApiKey];
    }

    private static async Task<byte[][]> GenerateWithOpenAiAsync(
        IReadOnlyList<string> chunks,
        GenerateAudioRecordingCommand command,
        string apiKey,
        string outputFormat,
        CancellationToken cancellationToken)
    {
        var voice = new GeneratedSpeechVoice(command.Voice);
        var format = MapOpenAiFormat(outputFormat);
        var audioClient = new AudioClient(command.Model, apiKey);
        var clipBytes = new byte[chunks.Count][];
        var parallelism = Math.Max(1, Math.Min(command.MaxParallelism, chunks.Count));

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
                    SpeedRatio = (float)command.Speed,
                    ResponseFormat = format,
                };

                if (!string.IsNullOrWhiteSpace(command.Instructions))
                {
                    options.Instructions = command.Instructions;
                }

                var result = await audioClient.GenerateSpeechAsync(pair.text, voice, options, ct);
                clipBytes[pair.index] = result.Value.ToMemory().ToArray();
            });

        return clipBytes;
    }

    private static async Task<byte[][]> GenerateWithGeminiAsync(
        IReadOnlyList<string> chunks,
        GenerateAudioRecordingCommand command,
        string apiKey,
        string outputFormat,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(600) };
        var googleAi = new GoogleAi(apiKey, client: httpClient);
        var model = googleAi.CreateGenerativeModel(command.Model);
        var clipBytes = new byte[chunks.Count][];
        var parallelism = Math.Max(1, Math.Min(command.MaxParallelism, chunks.Count));

        await Parallel.ForEachAsync(
            chunks.Select((text, index) => (text, index)),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelism,
                CancellationToken = cancellationToken,
            },
            async (pair, ct) =>
            {
                var prompt = !string.IsNullOrWhiteSpace(command.Instructions)
                    ? $"{command.Instructions}\n\n{pair.text}"
                    : pair.text;

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
                                    VoiceName = command.Voice,
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
                        $"Gemini TTS returned no audio data for chunk {pair.index}");
                }

                var pcmBytes = Convert.FromBase64String(inlineData.Data);
                var sampleRate = AudioConverter.ParseSampleRateFromMime(inlineData.MimeType);
                clipBytes[pair.index] = AudioConverter.ConvertPcm(pcmBytes, outputFormat, sampleRate);
            });

        return clipBytes;
    }

    private static GeneratedSpeechFormat MapOpenAiFormat(string format)
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
}
