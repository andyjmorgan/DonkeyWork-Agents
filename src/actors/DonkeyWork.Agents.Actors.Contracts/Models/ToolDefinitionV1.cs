namespace DonkeyWork.Agents.Actors.Contracts.Models;

public sealed class ToolDefinitionV1
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}
