using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface ISandboxCredentialMappingService
{
    Task<IReadOnlyList<SandboxCredentialMappingV1>> ListAsync(CancellationToken ct = default);

    Task<SandboxCredentialMappingV1?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SandboxCredentialMappingV1> CreateAsync(CreateSandboxCredentialMappingRequestV1 request, CancellationToken ct = default);

    Task<SandboxCredentialMappingV1> UpdateAsync(Guid id, UpdateSandboxCredentialMappingRequestV1 request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<ResolvedDomainCredentialV1?> ResolveForDomainAsync(string baseDomain, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetConfiguredDomainsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SandboxProviderStatusV1>> ListProviderStatusesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SandboxCredentialMappingV1>> CreateFromProviderAsync(OAuthProvider provider, CancellationToken ct = default);

    Task DeleteByProviderAsync(OAuthProvider provider, CancellationToken ct = default);
}
