using DonkeyWork.Agents.Actors.Contracts.Messages;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

/// <summary>
/// Persistent store for grain messages, replacing SeaweedFS-based grain state.
/// </summary>
public interface IGrainMessageStore
{
    /// <summary>
    /// Loads all messages for a grain, ordered by sequence number.
    /// Returns the messages and the next sequence number to use for appending.
    /// </summary>
    Task<(List<InternalMessage> Messages, int NextSequenceNumber)> LoadMessagesAsync(
        string grainKey, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Appends a single message to the grain's history at the given sequence number.
    /// Returns the next sequence number.
    /// </summary>
    Task<int> AppendMessageAsync(
        string grainKey, Guid userId, InternalMessage message, int sequenceNumber, CancellationToken ct = default);

    /// <summary>
    /// Deletes all messages at or after the given sequence number (for rollback on error).
    /// </summary>
    Task RollbackFromAsync(
        string grainKey, Guid userId, int fromSequenceNumber, CancellationToken ct = default);
}
