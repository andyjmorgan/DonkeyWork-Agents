using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Represents an external API key credential.
/// </summary>
public sealed class ExternalApiKey
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required ExternalApiKeyProvider Provider { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Dictionary of credential fields (e.g., ApiKey, Username, Password).
    /// Values are encrypted at rest.
    /// </summary>
    public required IReadOnlyDictionary<CredentialFieldType, string> Fields { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastUsedAt { get; init; }
}
