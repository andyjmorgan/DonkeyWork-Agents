namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;

/// <summary>
/// Marks a field as immutable - set on creation, read-only in properties panel.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ImmutableAttribute : Attribute
{
}
