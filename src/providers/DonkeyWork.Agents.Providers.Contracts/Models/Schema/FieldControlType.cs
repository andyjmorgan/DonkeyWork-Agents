using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// The type of UI control to render for a configuration field.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FieldControlType
{
    Slider,
    NumberInput,
    TextInput,
    Select,
    Toggle
}
