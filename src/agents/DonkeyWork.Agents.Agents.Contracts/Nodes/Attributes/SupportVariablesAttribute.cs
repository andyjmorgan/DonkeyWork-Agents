namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;

/// <summary>
/// Indicates that a field supports {{variable}} syntax for runtime resolution.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SupportVariablesAttribute : Attribute
{
}
