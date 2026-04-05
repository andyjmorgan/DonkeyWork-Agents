using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

public interface IExecutionStreamService
{
    IAsyncEnumerable<ExecutionEvent> ReadEventsAsync(Guid userId, Guid executionId, long offset = 0);

    Task DeleteStreamAsync(Guid userId, Guid executionId);
}
