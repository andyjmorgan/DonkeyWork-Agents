namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class ApiKeyItemV1
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Masked API key (e.g., dk_abc***xyz).
    /// </summary>
    public required string MaskedKey { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
