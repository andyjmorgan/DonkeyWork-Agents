namespace DonkeyWork.Agents.Actions.Contracts.Attributes;

/// <summary>
/// Marks a class as an action provider that contains executable action methods
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ActionProviderAttribute : Attribute
{
}

/// <summary>
/// Marks a method as an executable action implementation
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ActionMethodAttribute : Attribute
{
    /// <summary>
    /// The action type this method implements (must match ActionNodeAttribute.ActionType)
    /// </summary>
    public string ActionType { get; }

    public ActionMethodAttribute(string actionType)
    {
        ActionType = actionType;
    }
}

/// <summary>
/// Specifies which credential types are supported by this action
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class SupportedCredentialsAttribute : Attribute
{
    public string[] CredentialTypes { get; }

    public SupportedCredentialsAttribute(params string[] credentialTypes)
    {
        CredentialTypes = credentialTypes;
    }
}
