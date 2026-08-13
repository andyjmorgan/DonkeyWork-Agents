using DonkeyWork.Agents.Providers.Contracts.Models;

namespace DonkeyWork.Agents.Providers.Contracts.Services;

public interface ICustomModelService
{
    Task<IReadOnlyList<CustomModelV1>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomModelV1?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResolvedCustomModel?> ResolveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomModelV1> CreateAsync(CreateCustomModelRequestV1 request, CancellationToken cancellationToken = default);
    Task<CustomModelV1?> UpdateAsync(Guid id, UpdateCustomModelRequestV1 request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TestCustomModelResponseV1> TestAsync(TestCustomModelRequestV1 request, CancellationToken cancellationToken = default);
}
