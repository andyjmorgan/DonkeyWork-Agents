using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface IExternalApiKeyService
{
    Task<ExternalApiKey?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalApiKey>> GetByProviderAsync(Guid userId, ExternalApiKeyProvider provider, CancellationToken cancellationToken = default);

    Task<ExternalApiKey> CreateAsync(
        Guid userId,
        ExternalApiKeyProvider provider,
        string name,
        IDictionary<CredentialFieldType, string> fields,
        CancellationToken cancellationToken = default);

    Task<ExternalApiKey> UpdateAsync(
        Guid userId,
        Guid id,
        string? name,
        IDictionary<CredentialFieldType, string>? fields,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the decrypted API key value for a specific provider.
    /// Used internally for making API calls.
    /// </summary>
    Task<string?> GetApiKeyValueAsync(ExternalApiKeyProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets which LLM providers the user has credentials for.
    /// </summary>
    Task<IReadOnlyList<ExternalApiKeyProvider>> GetConfiguredLlmProvidersAsync(Guid userId, CancellationToken cancellationToken = default);
}
