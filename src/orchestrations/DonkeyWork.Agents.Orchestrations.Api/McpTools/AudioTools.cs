using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Orchestrations.Api.McpTools;

/// <summary>
/// MCP tools for managing audio recordings and collections.
/// Generation is asynchronous — <see cref="CreateAudioRecording"/> returns a recording ID
/// immediately and the caller polls <see cref="GetRecordingStatus"/> until Status = Ready.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class AudioTools
{
    private readonly IAudioGenerationService _audioGenerationService;
    private readonly IAudioCollectionService _collectionService;
    private readonly ITtsService _ttsService;

    public AudioTools(
        IAudioGenerationService audioGenerationService,
        IAudioCollectionService collectionService,
        ITtsService ttsService)
    {
        _audioGenerationService = audioGenerationService;
        _collectionService = collectionService;
        _ttsService = ttsService;
    }

    [McpServerTool(Name = "create_audio_recording", Title = "Create Audio Recording")]
    [Description(
        "Generate a new TTS audio recording from text. Returns a recording ID immediately with Status=Pending; " +
        "the chunk → TTS → concat → store pipeline runs in the background. Poll get_recording_status until Status=Ready. " +
        "Long text is automatically split to respect provider limits, generated in parallel, and stitched.")]
    public async Task<TtsRecordingV1?> CreateAudioRecording(
        [Description("The text to convert to speech. Markdown is supported and chunked on block boundaries.")] string text,
        [Description("Name for the recording.")] string name,
        [Description("TTS model (e.g. tts-1, gpt-4o-mini-tts, gemini-2.5-flash-preview-tts).")] string model,
        [Description("Voice for the TTS provider (e.g. alloy for OpenAI, Kore for Gemini).")] string voice,
        [Description("Optional collection (folder) UUID to drop the recording into.")] Guid? collectionId = null,
        [Description("Optional description for the recording.")] string? description = null,
        [Description("Optional chapter title when inside a collection.")] string? chapterTitle = null,
        [Description("Optional sequence number within the collection. If omitted, appends to end.")] int? sequenceNumber = null,
        [Description("Optional voice-style instructions (e.g. 'Speak warmly with moderate pacing').")] string? instructions = null,
        [Description("Output format: mp3 (default), wav, opus, aac, flac.")] string responseFormat = "mp3",
        CancellationToken ct = default)
    {
        var request = new StartAudioGenerationRequestV1
        {
            Text = text,
            Name = name,
            Description = description,
            Model = model,
            Voice = voice,
            Instructions = instructions,
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = chapterTitle,
            ResponseFormat = responseFormat,
        };

        var recordingId = await _audioGenerationService.StartGenerationAsync(request, ct);
        return await _ttsService.GetRecordingAsync(recordingId, ct);
    }

    [McpServerTool(Name = "get_recording_status", Title = "Get Recording Status", ReadOnly = true)]
    [Description(
        "Get the current status of an audio recording. Status progresses Pending → Generating → Ready (or Failed). " +
        "Progress is a [0,1] float. FilePath and SizeBytes are populated once Status=Ready.")]
    public async Task<TtsRecordingV1?> GetRecordingStatus(
        [Description("The recording ID returned from create_audio_recording.")] Guid recordingId,
        CancellationToken ct = default)
    {
        return await _ttsService.GetRecordingAsync(recordingId, ct);
    }

    [McpServerTool(Name = "list_audio_collections", Title = "List Audio Collections", ReadOnly = true)]
    [Description("List audio collections (folders) for the current user.")]
    public async Task<ListAudioCollectionsResponseV1> ListAudioCollections(
        [Description("Pagination offset (default 0).")] int? offset = null,
        [Description("Page size (default 20, max 100).")] int? limit = null,
        CancellationToken ct = default)
    {
        return await _collectionService.ListAsync(offset ?? 0, Math.Min(limit ?? 20, 100), ct);
    }

    [McpServerTool(Name = "get_audio_collection", Title = "Get Audio Collection", ReadOnly = true)]
    [Description("Get a single audio collection by ID.")]
    public async Task<AudioCollectionV1?> GetAudioCollection(
        [Description("Collection ID.")] Guid id,
        CancellationToken ct = default)
    {
        return await _collectionService.GetAsync(id, ct);
    }

    [McpServerTool(Name = "create_audio_collection", Title = "Create Audio Collection")]
    [Description("Create a new audio collection (folder) scoped to the current user.")]
    public async Task<AudioCollectionV1> CreateAudioCollection(
        [Description("Display name for the collection.")] string name,
        [Description("Optional description.")] string? description = null,
        [Description("Optional default voice applied to new recordings created in this collection.")] string? defaultVoice = null,
        [Description("Optional default TTS model applied to new recordings created in this collection.")] string? defaultModel = null,
        CancellationToken ct = default)
    {
        return await _collectionService.CreateAsync(new CreateAudioCollectionRequestV1
        {
            Name = name,
            Description = description,
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
        }, ct);
    }

    [McpServerTool(Name = "update_audio_collection", Title = "Update Audio Collection")]
    [Description("Update an existing audio collection. Omit fields to leave them unchanged.")]
    public async Task<AudioCollectionV1?> UpdateAudioCollection(
        [Description("Collection ID.")] Guid id,
        [Description("New name.")] string? name = null,
        [Description("New description.")] string? description = null,
        [Description("New default voice.")] string? defaultVoice = null,
        [Description("New default model.")] string? defaultModel = null,
        [Description("New cover image path.")] string? coverImagePath = null,
        CancellationToken ct = default)
    {
        return await _collectionService.UpdateAsync(id, new UpdateAudioCollectionRequestV1
        {
            Name = name,
            Description = description,
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
            CoverImagePath = coverImagePath,
        }, ct);
    }

    [McpServerTool(Name = "delete_audio_collection", Title = "Delete Audio Collection")]
    [Description(
        "Delete an audio collection. Recordings in the collection are preserved (become unfiled); " +
        "they are not cascade-deleted.")]
    public async Task<bool> DeleteAudioCollection(
        [Description("Collection ID.")] Guid id,
        CancellationToken ct = default)
    {
        return await _collectionService.DeleteAsync(id, ct);
    }
}
