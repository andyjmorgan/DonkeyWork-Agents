using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Response containing external API key details with full (unmasked) API key.
/// </summary>
public sealed class ExternalApiKeyResponseV1
{
    public required Guid Id { get; init; }

    public required ExternalApiKeyProvider Provider { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// The full API key value.
    /// </summary>
    public required string ApiKey { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastUsedAt { get; init; }
}
