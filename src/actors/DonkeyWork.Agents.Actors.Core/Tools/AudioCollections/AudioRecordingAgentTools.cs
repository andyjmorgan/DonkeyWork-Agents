using System.ComponentModel;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.AudioCollections;

public sealed class AudioRecordingAgentTools
{
    private readonly ITtsService _ttsService;
    private readonly IAudioCollectionService _collectionService;

    public AudioRecordingAgentTools(
        ITtsService ttsService,
        IAudioCollectionService collectionService)
    {
        _ttsService = ttsService;
        _collectionService = collectionService;
    }

    [AgentTool("audio_recordings_list", DisplayName = "List Audio Recordings")]
    [Description(
        "List audio recordings. Pass collectionId to scope to a single collection (returned in sequence order). " +
        "Pass unfiledOnly=true to list recordings not assigned to any collection. " +
        "Omit both for a flat list of every recording, newest first.")]
    public async Task<ToolResult> ListRecordings(
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
            var result = await _collectionService.ListRecordingsAsync(collectionId.Value, pageOffset, pageLimit, ct);
            return result is not null
                ? ToolResult.Json(result)
                : ToolResult.NotFound("AudioCollection", collectionId.Value);
        }

        var flat = await _ttsService.ListRecordingsAsync(pageOffset, pageLimit, unfiledOnly, ct);
        return ToolResult.Json(flat);
    }

    [AgentTool("audio_recordings_get", DisplayName = "Get Audio Recording")]
    [Description(
        "Get a single audio recording by ID. Returns full metadata: status (Pending/Generating/Ready/Failed), " +
        "progress, file path/size when ready, collection ID, transcript, and playback state.")]
    public async Task<ToolResult> GetRecording(
        [Description("The recording ID")] Guid recordingId,
        CancellationToken ct = default)
    {
        var recording = await _ttsService.GetRecordingAsync(recordingId, ct);
        return recording is not null
            ? ToolResult.Json(recording)
            : ToolResult.NotFound("Recording", recordingId);
    }

    [AgentTool("audio_recordings_delete", DisplayName = "Delete Audio Recording")]
    [Description("Permanently delete an audio recording and its underlying audio file.")]
    public async Task<ToolResult> DeleteRecording(
        [Description("The recording ID")] Guid recordingId,
        CancellationToken ct = default)
    {
        var deleted = await _ttsService.DeleteRecordingAsync(recordingId, ct);
        return deleted
            ? ToolResult.Success($"Recording '{recordingId}' deleted successfully.")
            : ToolResult.NotFound("Recording", recordingId);
    }

    [AgentTool("audio_recordings_move", DisplayName = "Move Audio Recording")]
    [Description(
        "Move a recording to a different collection, or unfile it. " +
        "Pass collectionId=null to remove it from its current collection without assigning a new one.")]
    public async Task<ToolResult> MoveRecording(
        [Description("The recording ID")] Guid recordingId,
        [Description("Target collection ID, or null to unfile the recording.")] Guid? collectionId = null,
        [Description("Optional sequence number within the target collection.")] int? sequenceNumber = null,
        [Description("Optional chapter title within the target collection.")] string? chapterTitle = null,
        CancellationToken ct = default)
    {
        var recording = await _collectionService.MoveRecordingAsync(recordingId, new MoveRecordingToCollectionRequestV1
        {
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = chapterTitle,
        }, ct);
        return recording is not null
            ? ToolResult.Json(recording)
            : ToolResult.NotFound("Recording", recordingId);
    }
}
