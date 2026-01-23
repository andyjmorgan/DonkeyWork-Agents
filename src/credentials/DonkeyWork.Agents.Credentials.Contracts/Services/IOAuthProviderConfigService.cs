using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface IOAuthProviderConfigService
{
    Task<OAuthProviderConfig?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<OAuthProviderConfig?> GetByProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthProviderConfig>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<OAuthProviderConfig> CreateAsync(
        Guid userId,
        OAuthProvider provider,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthProviderConfig> UpdateAsync(
        Guid userId,
        Guid id,
        string? clientId,
        string? clientSecret,
        string? redirectUri,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
