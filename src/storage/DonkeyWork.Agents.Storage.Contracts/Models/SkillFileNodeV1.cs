namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class SkillFileNodeV1
{
    public required string Name { get; init; }
    public required bool IsDirectory { get; init; }
    public IReadOnlyList<SkillFileNodeV1>? Children { get; init; }
}
