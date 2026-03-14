namespace DonkeyWork.Agents.Actors.Contracts.Models;

public sealed class ToolGroupDefinitionV1
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<ToolDefinitionV1> Tools { get; init; } = [];
}
