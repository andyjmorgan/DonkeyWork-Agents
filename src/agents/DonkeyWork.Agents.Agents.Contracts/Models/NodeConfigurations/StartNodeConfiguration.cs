namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Configuration for a Start node.
/// </summary>
public sealed class StartNodeConfiguration : NodeConfiguration
{
    // Only inherits Name from base class
    // InputSchema is at the AgentVersion level, not per node
}
