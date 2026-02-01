namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Maps a property to specific credential types.
/// Used to indicate that a field should be populated with credentials of the specified types.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CredentialMappingAttribute : Attribute
{
    /// <summary>
    /// The credential types that this field accepts (e.g., "Anthropic", "OpenAI", "Google").
    /// </summary>
    public string[] CredentialTypes { get; }

    public CredentialMappingAttribute(params string[] credentialTypes)
    {
        CredentialTypes = credentialTypes;
    }
}
