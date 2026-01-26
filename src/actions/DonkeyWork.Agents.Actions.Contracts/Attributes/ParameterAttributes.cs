namespace DonkeyWork.Agents.Actions.Contracts.Attributes;

/// <summary>
/// Editor types for parameter UI controls
/// </summary>
public enum EditorType
{
    /// <summary>
    /// Standard text input
    /// </summary>
    Text,

    /// <summary>
    /// Multi-line textarea
    /// </summary>
    TextArea,

    /// <summary>
    /// Code editor with syntax highlighting
    /// </summary>
    Code,

    /// <summary>
    /// Dropdown/select
    /// </summary>
    Dropdown,

    /// <summary>
    /// Number input with spinner
    /// </summary>
    Number,

    /// <summary>
    /// Checkbox
    /// </summary>
    Checkbox,

    /// <summary>
    /// Slider control
    /// </summary>
    Slider,

    /// <summary>
    /// Date/time picker
    /// </summary>
    DateTime,

    /// <summary>
    /// JSON editor
    /// </summary>
    Json
}

/// <summary>
/// Specifies the UI editor type for a parameter
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EditorTypeAttribute : Attribute
{
    public EditorType EditorType { get; }

    public EditorTypeAttribute(EditorType editorType)
    {
        EditorType = editorType;
    }
}

/// <summary>
/// Indicates that a parameter supports variable expressions ({{...}})
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SupportVariablesAttribute : Attribute
{
}

/// <summary>
/// Indicates that a parameter should render as a slider control
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class SliderAttribute : Attribute
{
    /// <summary>
    /// Step size for the slider (e.g., 0.01 for decimal precision)
    /// </summary>
    public double Step { get; set; } = 1;
}

/// <summary>
/// Maps a parameter to specific credential types
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CredentialMappingAttribute : Attribute
{
    public string[] CredentialTypes { get; }

    public CredentialMappingAttribute(params string[] credentialTypes)
    {
        CredentialTypes = credentialTypes;
    }
}

/// <summary>
/// Specifies that parameter options should be loaded dynamically
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class LoadOptionsAttribute : Attribute
{
    /// <summary>
    /// Name of the method that loads options
    /// </summary>
    public string LoaderMethod { get; }

    public LoadOptionsAttribute(string loaderMethod)
    {
        LoaderMethod = loaderMethod;
    }
}

/// <summary>
/// Makes a parameter's visibility depend on another parameter's value
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DependsOnAttribute : Attribute
{
    /// <summary>
    /// Name of the parameter this depends on
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Condition for showing this parameter (e.g., "Type == 'custom'")
    /// </summary>
    public string? ShowIf { get; set; }

    public DependsOnAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }
}
