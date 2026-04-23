using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Entry point for agent-initiated (MCP-tool / HTTP) audio generation.
/// Returns a recording ID immediately; the chunked TTS pipeline runs on a
/// background Wolverine handler and updates the recording as it progresses.
/// </summary>
public interface IAudioGenerationService
{
    /// <summary>
    /// Register a Pending recording and enqueue the generation command.
    /// Returns the new recording ID; callers poll <see cref="ITtsService.GetRecordingAsync"/>
    /// (or subscribe to SignalR) for status progression.
    /// </summary>
    Task<Guid> StartGenerationAsync(StartAudioGenerationRequestV1 request, CancellationToken cancellationToken = default);
}
