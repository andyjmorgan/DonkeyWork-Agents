using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Services;

public interface IAgentDefinitionService
{
    Task<IReadOnlyList<AgentDefinitionSummaryV1>> ListAsync(CancellationToken cancellationToken = default);
    Task<AgentDefinitionDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentDefinitionDetailsV1> CreateAsync(CreateAgentDefinitionRequestV1 request, CancellationToken cancellationToken = default);
    Task<AgentDefinitionDetailsV1?> UpdateAsync(Guid id, UpdateAgentDefinitionRequestV1 request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NaviAgentDefinitionV1>> GetNaviConnectedAsync(CancellationToken cancellationToken = default);
}
