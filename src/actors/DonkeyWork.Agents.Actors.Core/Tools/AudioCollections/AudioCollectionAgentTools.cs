using System.ComponentModel;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.AudioCollections;

public sealed class AudioCollectionAgentTools
{
    private readonly IAudioCollectionService _collectionService;

    public AudioCollectionAgentTools(IAudioCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    [AgentTool("audio_collections_list", DisplayName = "List Audio Collections")]
    [Description("List audio collections (folders of mini-podcasts) for the current user.")]
    public async Task<ToolResult> ListCollections(
        [Description("Pagination offset (default 0).")] int? offset = null,
        [Description("Page size (default 20, max 100).")] int? limit = null,
        CancellationToken ct = default)
    {
        var result = await _collectionService.ListAsync(offset ?? 0, Math.Min(limit ?? 20, 100), ct);
        return ToolResult.Json(result);
    }

    [AgentTool("audio_collections_get", DisplayName = "Get Audio Collection")]
    [Description("Get a single audio collection by ID.")]
    public async Task<ToolResult> GetCollection(
        [Description("The collection ID")] Guid collectionId,
        CancellationToken ct = default)
    {
        var collection = await _collectionService.GetAsync(collectionId, ct);
        return collection is not null
            ? ToolResult.Json(collection)
            : ToolResult.NotFound("AudioCollection", collectionId);
    }

    [AgentTool("audio_collections_create", DisplayName = "Create Audio Collection")]
    [Description("Create a new audio collection (folder) scoped to the current user.")]
    public async Task<ToolResult> CreateCollection(
        [Description("Display name for the collection.")] string name,
        [Description("Optional description.")] string? description = null,
        [Description("Optional default voice applied to new recordings created in this collection.")] string? defaultVoice = null,
        [Description("Optional default TTS model applied to new recordings created in this collection.")] string? defaultModel = null,
        CancellationToken ct = default)
    {
        var collection = await _collectionService.CreateAsync(new CreateAudioCollectionRequestV1
        {
            Name = name,
            Description = description,
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
        }, ct);
        return ToolResult.Json(collection);
    }

    [AgentTool("audio_collections_update", DisplayName = "Update Audio Collection")]
    [Description("Update an existing audio collection. Omit fields to leave them unchanged.")]
    public async Task<ToolResult> UpdateCollection(
        [Description("The collection ID")] Guid collectionId,
        [Description("New name.")] string? name = null,
        [Description("New description.")] string? description = null,
        [Description("New default voice.")] string? defaultVoice = null,
        [Description("New default model.")] string? defaultModel = null,
        [Description("New cover image path.")] string? coverImagePath = null,
        CancellationToken ct = default)
    {
        var collection = await _collectionService.UpdateAsync(collectionId, new UpdateAudioCollectionRequestV1
        {
            Name = name,
            Description = description,
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
            CoverImagePath = coverImagePath,
        }, ct);
        return collection is not null
            ? ToolResult.Json(collection)
            : ToolResult.NotFound("AudioCollection", collectionId);
    }

    [AgentTool("audio_collections_delete", DisplayName = "Delete Audio Collection")]
    [Description(
        "Delete an audio collection. Recordings in the collection are preserved (become unfiled); " +
        "they are not cascade-deleted.")]
    public async Task<ToolResult> DeleteCollection(
        [Description("The collection ID")] Guid collectionId,
        CancellationToken ct = default)
    {
        var deleted = await _collectionService.DeleteAsync(collectionId, ct);
        return deleted
            ? ToolResult.Success($"Audio collection '{collectionId}' deleted successfully.")
            : ToolResult.NotFound("AudioCollection", collectionId);
    }

    [AgentTool("audio_collections_list_recordings", DisplayName = "List Recordings in Collection")]
    [Description("List recordings in an audio collection, ordered by sequence/chapter.")]
    public async Task<ToolResult> ListRecordings(
        [Description("The collection ID")] Guid collectionId,
        [Description("Pagination offset (default 0).")] int? offset = null,
        [Description("Page size (default 50, max 100).")] int? limit = null,
        CancellationToken ct = default)
    {
        var result = await _collectionService.ListRecordingsAsync(
            collectionId,
            offset ?? 0,
            Math.Min(limit ?? 50, 100),
            ct);
        return result is not null
            ? ToolResult.Json(result)
            : ToolResult.NotFound("AudioCollection", collectionId);
    }
}
