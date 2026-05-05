using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

public interface ITtsService
{
    Task<ListRecordingsResponseV1> ListRecordingsAsync(int offset, int limit, bool unfiledOnly = false, CancellationToken cancellationToken = default);
    Task<TtsRecordingV1?> GetRecordingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)?> DownloadAudioAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a short-lived presigned URL the browser can hit directly to stream the audio,
    /// or null if direct streaming is not available (filesystem-backed storage in local dev).
    /// </summary>
    Task<string?> GetAudioStreamUrlAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TtsPlaybackV1> GetPlaybackAsync(Guid recordingId, CancellationToken cancellationToken = default);
    Task<TtsPlaybackV1?> UpdatePlaybackAsync(Guid recordingId, UpdatePlaybackRequestV1 request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRecordingAsync(Guid id, CancellationToken cancellationToken = default);
}
