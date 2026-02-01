using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// The type of UI control to render for a configuration field.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FieldControlType
{
    /// <summary>
    /// Slider control for numeric values with min/max/step.
    /// </summary>
    Slider,

    /// <summary>
    /// Number input with optional min/max constraints.
    /// </summary>
    NumberInput,

    /// <summary>
    /// Single-line text input.
    /// </summary>
    TextInput,

    /// <summary>
    /// Multi-line text area.
    /// </summary>
    TextArea,

    /// <summary>
    /// Dropdown select control.
    /// </summary>
    Select,

    /// <summary>
    /// Boolean toggle switch.
    /// </summary>
    Toggle,

    /// <summary>
    /// Credential selector dropdown.
    /// </summary>
    Credential,

    /// <summary>
    /// Code editor with syntax highlighting.
    /// </summary>
    Code,

    /// <summary>
    /// JSON editor.
    /// </summary>
    Json,

    /// <summary>
    /// Key-value pair list editor.
    /// </summary>
    KeyValueList
}
