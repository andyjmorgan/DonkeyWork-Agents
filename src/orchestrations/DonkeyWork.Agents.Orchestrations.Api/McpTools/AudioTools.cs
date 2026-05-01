using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Orchestrations.Api.McpTools;

/// <summary>
/// MCP tools for browsing and managing audio collections and recordings.
/// Recording <em>creation</em> is intentionally not exposed — it is driven by orchestrations
/// (TTS + Store Audio nodes) and routed through them as the only authoring path.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class AudioTools
{
    private readonly ITtsService _ttsService;
    private readonly IAudioCollectionService _collectionService;

    public AudioTools(
        ITtsService ttsService,
        IAudioCollectionService collectionService)
    {
        _ttsService = ttsService;
        _collectionService = collectionService;
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

    [McpServerTool(Name = "list_audio_recordings", Title = "List Audio Recordings", ReadOnly = true)]
    [Description(
        "List audio recordings. Pass collectionId to scope to one collection (recordings come back ordered by sequence). " +
        "Pass unfiledOnly=true to list recordings that are not in any collection. " +
        "Omit both for a flat list of every recording, newest first.")]
    public async Task<ListRecordingsResponseV1?> ListAudioRecordings(
        [Description("Optional collection ID to scope the listing to one collection.")] Guid? collectionId = null,
        [Description("If true, only return recordings not assigned to any collection. Ignored when collectionId is set.")] bool unfiledOnly = false,
        [Description("Pagination offset (default 0).")] int? offset = null,
        [Description("Page size (default 20, max 100).")] int? limit = null,
        CancellationToken ct = default)
    {
        var pageOffset = offset ?? 0;
        var pageLimit = Math.Min(limit ?? 20, 100);

        if (collectionId.HasValue)
        {
            return await _collectionService.ListRecordingsAsync(collectionId.Value, pageOffset, pageLimit, ct);
        }

        return await _ttsService.ListRecordingsAsync(pageOffset, pageLimit, unfiledOnly, ct);
    }

    [McpServerTool(Name = "get_audio_recording", Title = "Get Audio Recording", ReadOnly = true)]
    [Description(
        "Get a single audio recording by ID. Returns full metadata: Status (Pending/Generating/Ready/Failed), " +
        "Progress, FilePath/SizeBytes (when Ready), CollectionId, transcript, and playback state.")]
    public async Task<TtsRecordingV1?> GetAudioRecording(
        [Description("The recording ID.")] Guid recordingId,
        CancellationToken ct = default)
    {
        return await _ttsService.GetRecordingAsync(recordingId, ct);
    }

    [McpServerTool(Name = "delete_audio_recording", Title = "Delete Audio Recording")]
    [Description("Permanently delete an audio recording and its underlying audio file.")]
    public async Task<bool> DeleteAudioRecording(
        [Description("The recording ID.")] Guid recordingId,
        CancellationToken ct = default)
    {
        return await _ttsService.DeleteRecordingAsync(recordingId, ct);
    }

    [McpServerTool(Name = "move_audio_recording", Title = "Move Audio Recording")]
    [Description(
        "Move a recording to a different collection, or unfile it. " +
        "Pass collectionId=null to remove it from its current collection without assigning a new one.")]
    public async Task<TtsRecordingV1?> MoveAudioRecording(
        [Description("The recording ID.")] Guid recordingId,
        [Description("Target collection ID, or null to unfile the recording.")] Guid? collectionId = null,
        [Description("Optional sequence number within the target collection.")] int? sequenceNumber = null,
        [Description("Optional chapter title within the target collection.")] string? chapterTitle = null,
        CancellationToken ct = default)
    {
        return await _collectionService.MoveRecordingAsync(recordingId, new MoveRecordingToCollectionRequestV1
        {
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = chapterTitle,
        }, ct);
    }
}
