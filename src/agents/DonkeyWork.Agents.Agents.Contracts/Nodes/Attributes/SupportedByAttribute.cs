namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;

/// <summary>
/// Specifies which models support this field.
/// If absent, field is available for all models of this provider.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SupportedByAttribute : Attribute
{
    /// <summary>
    /// The model identifiers that support this field.
    /// </summary>
    public string[] ModelIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SupportedByAttribute"/> class.
    /// </summary>
    /// <param name="modelIds">The model identifiers that support this field.</param>
    public SupportedByAttribute(params string[] modelIds)
    {
        ModelIds = modelIds;
    }
}
