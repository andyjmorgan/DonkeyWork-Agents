namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class GetApiKeyResponseV1
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The full API key value.
    /// </summary>
    public required string Key { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
