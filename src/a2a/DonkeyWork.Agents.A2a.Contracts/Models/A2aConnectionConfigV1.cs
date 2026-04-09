namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aConnectionConfigV1
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required string Address { get; init; }

    public int? TimeoutSeconds { get; init; }

    public Dictionary<string, string> Headers { get; init; } = new();
}
