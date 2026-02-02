namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;

/// <summary>
/// Marks a class as a node provider that contains methods for executing node types.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NodeProviderAttribute : Attribute
{
}
