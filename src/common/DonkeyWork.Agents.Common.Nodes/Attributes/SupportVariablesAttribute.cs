namespace DonkeyWork.Agents.Common.Nodes.Attributes;

/// <summary>
/// Indicates that a field supports {{variable}} syntax for runtime resolution.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SupportVariablesAttribute : Attribute
{
}
