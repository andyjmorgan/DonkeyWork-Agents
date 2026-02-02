using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Singleton service for managing execution streams.
/// Handles stream lifecycle and reading events.
/// For writing events, use the scoped IExecutionStreamWriter.
/// </summary>
public interface IExecutionStreamService
{
    /// <summary>
    /// Creates a new stream for an execution.
    /// </summary>
    Task CreateStreamAsync(Guid executionId);

    /// <summary>
    /// Reads events from an execution stream.
    /// </summary>
    IAsyncEnumerable<ExecutionEvent> ReadEventsAsync(Guid executionId, long offset = 0);

    /// <summary>
    /// Deletes an execution stream.
    /// </summary>
    Task DeleteStreamAsync(Guid executionId);
}
