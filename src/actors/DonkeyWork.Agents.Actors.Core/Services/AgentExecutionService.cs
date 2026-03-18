using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Services;

/// <summary>
/// API-facing service wrapping the execution repository and message store.
/// Uses IIdentityContext for user isolation.
/// </summary>
public sealed class AgentExecutionService : IAgentExecutionService
{
    private readonly IAgentExecutionRepository _repository;
    private readonly IGrainMessageStore _messageStore;
    private readonly IIdentityContext _identityContext;

    public AgentExecutionService(
        IAgentExecutionRepository repository,
        IGrainMessageStore messageStore,
        IIdentityContext identityContext)
    {
        _repository = repository;
        _messageStore = messageStore;
        _identityContext = identityContext;
    }

    public Task<AgentExecutionDetailV1?> GetByIdAsync(Guid executionId, CancellationToken ct = default)
    {
        return _repository.GetByIdAsync(executionId, _identityContext.UserId, ct);
    }

    public Task<IReadOnlyList<AgentExecutionSummaryV1>> ListByConversationAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        return _repository.ListByConversationAsync(conversationId, _identityContext.UserId, ct);
    }

    public async Task<GetAgentExecutionMessagesResponseV1?> GetMessagesAsync(Guid executionId, CancellationToken ct = default)
    {
        var execution = await _repository.GetByIdAsync(executionId, _identityContext.UserId, ct);
        if (execution is null)
            return null;

        var (messages, _) = await _messageStore.LoadMessagesAsync(
            execution.GrainKey, _identityContext.UserId, ct);

        return new GetAgentExecutionMessagesResponseV1 { Messages = messages };
    }
}
