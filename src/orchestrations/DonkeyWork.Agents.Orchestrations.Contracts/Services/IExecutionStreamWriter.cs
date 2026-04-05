using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Scoped service for writing events to the execution stream.
/// Lives for the duration of a single execution.
/// </summary>
public interface IExecutionStreamWriter : IAsyncDisposable
{
    /// <summary>
    /// Initializes the writer for a specific execution.
    /// Must be called before writing events.
    /// </summary>
    Task InitializeAsync(Guid userId, Guid executionId);

    /// <summary>
    /// Writes an event to the execution stream.
    /// Waits for broker confirmation before returning.
    /// </summary>
    Task WriteEventAsync(ExecutionEvent evt);
}
