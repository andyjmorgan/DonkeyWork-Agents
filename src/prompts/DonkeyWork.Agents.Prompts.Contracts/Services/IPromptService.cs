using DonkeyWork.Agents.Prompts.Contracts.Models;

namespace DonkeyWork.Agents.Prompts.Contracts.Services;

public interface IPromptService
{
    Task<IReadOnlyList<PromptSummaryV1>> ListAsync(CancellationToken cancellationToken = default);
    Task<PromptDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PromptDetailsV1> CreateAsync(CreatePromptRequestV1 request, CancellationToken cancellationToken = default);
    Task<PromptDetailsV1?> UpdateAsync(Guid id, UpdatePromptRequestV1 request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
