namespace DonkeyWork.Agents.Actors.Core.Providers;

internal class InternalToolDefinition
{
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public object? InputSchema { get; set; }
}
