using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface IUserApiKeyService
{
    /// <summary>
    /// Lists all API keys for the current user with masked key values.
    /// </summary>
    Task<(IReadOnlyList<UserApiKey> Items, int TotalCount)> ListAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single API key by ID with full (unmasked) key value.
    /// </summary>
    Task<UserApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key. Returns the key with full (unmasked) value.
    /// </summary>
    Task<UserApiKey> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an API key.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an API key and returns the associated user ID if valid.
    /// </summary>
    Task<Guid?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}
