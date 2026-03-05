namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class SkillItemV1
{
    public required string Name { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
