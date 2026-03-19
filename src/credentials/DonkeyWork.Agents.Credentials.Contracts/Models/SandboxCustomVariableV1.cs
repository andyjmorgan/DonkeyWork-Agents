namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class SandboxCustomVariableV1
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Value { get; init; }

    public required bool IsSecret { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}
