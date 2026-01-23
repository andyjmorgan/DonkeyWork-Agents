using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// List item for external API key with masked API key value.
/// </summary>
public sealed class ExternalApiKeyItemV1
{
    public required Guid Id { get; init; }

    public required ExternalApiKeyProvider Provider { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// Masked API key (e.g., sk-abc***xyz).
    /// </summary>
    public required string MaskedApiKey { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastUsedAt { get; init; }
}
