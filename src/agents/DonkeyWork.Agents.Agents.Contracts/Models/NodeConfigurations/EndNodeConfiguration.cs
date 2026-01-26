namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Configuration for an End node.
/// </summary>
public sealed class EndNodeConfiguration : NodeConfiguration
{
    // Only inherits Name from base class
    // OutputSchema is at the AgentVersion level, not per node
}
