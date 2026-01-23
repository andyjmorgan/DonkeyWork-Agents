namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal tool context.
/// </summary>
internal class InternalToolContext
{
    public List<InternalToolDefinition> Tools { get; set; } = [];
}

internal class InternalToolDefinition
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? InputSchema { get; set; }
}
