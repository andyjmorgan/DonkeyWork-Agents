using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface IOAuthTokenService
{
    Task<OAuthToken?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<OAuthToken?> GetByProviderAsync(Guid userId, OAuthProvider provider, CancellationToken cancellationToken = default);

    Task<OAuthToken> StoreTokenAsync(
        Guid userId,
        OAuthProvider provider,
        string externalUserId,
        string email,
        string accessToken,
        string refreshToken,
        IEnumerable<string> scopes,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<OAuthToken> RefreshTokenAsync(Guid id, string newAccessToken, string newRefreshToken, DateTimeOffset newExpiresAt, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tokens that are expiring within the specified time window.
    /// Used by the token refresh worker.
    /// </summary>
    Task<IReadOnlyList<OAuthToken>> GetExpiringTokensAsync(TimeSpan expirationWindow, CancellationToken cancellationToken = default);
}
