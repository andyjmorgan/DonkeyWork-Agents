using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

/// <summary>
/// Defines the UI control types for node configuration fields.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControlType
{
    /// <summary>
    /// Single-line text input.
    /// </summary>
    Text,

    /// <summary>
    /// Multi-line text area.
    /// </summary>
    TextArea,

    /// <summary>
    /// List of multi-line text areas (for SystemPrompts[], UserMessages[]).
    /// </summary>
    TextAreaList,

    /// <summary>
    /// Numeric input with optional min/max constraints.
    /// </summary>
    Number,

    /// <summary>
    /// Slider control for numeric values with min/max/step.
    /// </summary>
    Slider,

    /// <summary>
    /// Dropdown select control.
    /// </summary>
    Select,

    /// <summary>
    /// Boolean toggle switch.
    /// </summary>
    Toggle,

    /// <summary>
    /// Monaco code editor with syntax highlighting.
    /// </summary>
    Code,

    /// <summary>
    /// Monaco JSON editor.
    /// </summary>
    Json,

    /// <summary>
    /// Key-value pair list editor.
    /// </summary>
    KeyValueList,

    /// <summary>
    /// Credential selector dropdown.
    /// </summary>
    Credential
}
