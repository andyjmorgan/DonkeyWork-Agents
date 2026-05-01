using Asp.Versioning;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Orchestrations.Api.Controllers;

/// <summary>
/// Manage TTS recordings and playback state.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tts")]
[Authorize]
[Produces("application/json")]
public class TtsController : ControllerBase
{
    private readonly ITtsService _ttsService;
    private readonly IAudioCollectionService _audioCollectionService;
    private readonly IAudioGenerationService _audioGenerationService;

    public TtsController(
        ITtsService ttsService,
        IAudioCollectionService audioCollectionService,
        IAudioGenerationService audioGenerationService)
    {
        _ttsService = ttsService;
        _audioCollectionService = audioCollectionService;
        _audioGenerationService = audioGenerationService;
    }

    /// <summary>
    /// List all TTS recordings for the current user.
    /// </summary>
    [HttpGet("recordings")]
    [ProducesResponseType<ListRecordingsResponseV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRecordings(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        [FromQuery] bool unfiled = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _ttsService.ListRecordingsAsync(offset, limit, unfiled, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific TTS recording by ID.
    /// </summary>
    [HttpGet("recordings/{id:guid}")]
    [ProducesResponseType<TtsRecordingV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecording(Guid id, CancellationToken cancellationToken)
    {
        var recording = await _ttsService.GetRecordingAsync(id, cancellationToken);
        return recording == null ? NotFound() : Ok(recording);
    }

    /// <summary>
    /// Stream the audio file for a recording.
    /// </summary>
    [HttpGet("recordings/{id:guid}/audio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudio(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ttsService.DownloadAudioAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Update playback state for a recording (last write wins).
    /// </summary>
    [HttpPut("recordings/{id:guid}/playback")]
    [ProducesResponseType<TtsPlaybackV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayback(
        Guid id,
        [FromBody] UpdatePlaybackRequestV1 request,
        CancellationToken cancellationToken)
    {
        var result = await _ttsService.UpdatePlaybackAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Get playback state for a recording.
    /// </summary>
    [HttpGet("recordings/{id:guid}/playback")]
    [ProducesResponseType<TtsPlaybackV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayback(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ttsService.GetPlaybackAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a TTS recording.
    /// </summary>
    [HttpDelete("recordings/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecording(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _ttsService.DeleteRecordingAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Kick off generation of a new recording. Returns the Pending recording immediately;
    /// the chunked TTS → concat → upload pipeline runs on a background Wolverine handler.
    /// Poll <see cref="GetRecording"/> or subscribe to SignalR for Status transitions.
    /// </summary>
    [HttpPost("recordings/generate")]
    [ProducesResponseType<TtsRecordingV1>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartGeneration(
        [FromBody] StartAudioGenerationRequestV1 request,
        CancellationToken cancellationToken)
    {
        var recordingId = await _audioGenerationService.StartGenerationAsync(request, cancellationToken);
        var recording = await _ttsService.GetRecordingAsync(recordingId, cancellationToken);
        return AcceptedAtAction(nameof(GetRecording), new { id = recordingId, version = "1" }, recording);
    }

    /// <summary>
    /// Move a recording between collections (or to/from the unfiled list).
    /// </summary>
    [HttpPut("recordings/{id:guid}/collection")]
    [ProducesResponseType<TtsRecordingV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveRecording(
        Guid id,
        [FromBody] MoveRecordingToCollectionRequestV1 request,
        CancellationToken cancellationToken)
    {
        var result = await _audioCollectionService.MoveRecordingAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }
}
