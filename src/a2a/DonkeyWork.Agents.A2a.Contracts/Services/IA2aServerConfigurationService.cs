using DonkeyWork.Agents.A2a.Contracts.Models;

namespace DonkeyWork.Agents.A2a.Contracts.Services;

public interface IA2aServerConfigurationService
{
    Task<A2aServerDetailsV1> CreateAsync(CreateA2aServerRequestV1 request, CancellationToken cancellationToken = default);

    Task<A2aServerDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<A2aServerSummaryV1>> ListAsync(CancellationToken cancellationToken = default);

    Task<A2aServerDetailsV1?> UpdateAsync(Guid id, UpdateA2aServerRequestV1 request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<A2aConnectionConfigV1>> GetEnabledConnectionConfigsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<A2aConnectionConfigV1>> GetNaviConnectionConfigsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<A2aConnectionConfigV1>> GetMcpPublishedConnectionConfigsAsync(CancellationToken cancellationToken = default);

    Task<A2aConnectionConfigV1?> GetConnectionConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
