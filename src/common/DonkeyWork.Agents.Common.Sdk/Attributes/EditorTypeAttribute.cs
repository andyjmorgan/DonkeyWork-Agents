namespace DonkeyWork.Agents.Common.Sdk.Attributes;

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
    /// List of multi-line textareas (for arrays like SystemPrompts[], UserMessages[])
    /// </summary>
    TextAreaList,

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
    /// Checkbox/toggle
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
    Json,

    /// <summary>
    /// Key-value list editor
    /// </summary>
    KeyValueList,

    /// <summary>
    /// Credential selector
    /// </summary>
    Credential
}

/// <summary>
/// Specifies the UI editor type for a parameter.
/// If not specified, the editor type is inferred from the property type and other attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorTypeAttribute : Attribute
{
    public EditorType EditorType { get; }

    public EditorTypeAttribute(EditorType editorType)
    {
        EditorType = editorType;
    }
}
