using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

public interface ITtsService
{
    Task<ListRecordingsResponseV1> ListRecordingsAsync(int offset, int limit, CancellationToken cancellationToken = default);
    Task<TtsRecordingV1?> GetRecordingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)?> DownloadAudioAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TtsPlaybackV1> GetPlaybackAsync(Guid recordingId, CancellationToken cancellationToken = default);
    Task<TtsPlaybackV1?> UpdatePlaybackAsync(Guid recordingId, UpdatePlaybackRequestV1 request, CancellationToken cancellationToken = default);
    Task<bool> DeleteRecordingAsync(Guid id, CancellationToken cancellationToken = default);
}
