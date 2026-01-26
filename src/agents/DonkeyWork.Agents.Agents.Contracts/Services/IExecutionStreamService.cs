using DonkeyWork.Agents.Agents.Contracts.Models.Events;

namespace DonkeyWork.Agents.Agents.Contracts.Services;

public interface IExecutionStreamService
{
    Task CreateStreamAsync(Guid executionId);
    Task WriteEventAsync(Guid executionId, ExecutionEvent evt);
    IAsyncEnumerable<ExecutionEvent> ReadEventsAsync(Guid executionId, long offset = 0);
    Task DeleteStreamAsync(Guid executionId);
}
