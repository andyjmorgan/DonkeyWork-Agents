using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Gemini Text-to-Speech node.
/// Uses Google Gemini TTS models which support larger context windows (8K tokens).
/// </summary>
[Node(
    DisplayName = "Gemini Text to Speech",
    Description = "Generate speech audio from text using Google Gemini TTS",
    Category = "Audio",
    Icon = "volume-2",
    Color = "pink")]
public sealed class GeminiTextToSpeechNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.GeminiTextToSpeech;

    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 20)]
    [Tab("Basic", Order = 1)]
    [Immutable]
    public required string Model { get; init; }

    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Select, Order = 30)]
    [Tab("Basic", Order = 1)]
    [SelectOptions(
        "Achernar", "Achird", "Algenib", "Algieba", "Alnilam", "Aoede", "Autonoe",
        "Callirrhoe", "Charon", "Despina", "Enceladus", "Erinome", "Fenrir",
        "Gacrux", "Iapetus", "Kore", "Laomedeia", "Leda", "Orus",
        "Puck", "Pulcherrima", "Rasalgethi", "Sadachbia", "Sadaltager",
        "Schedar", "Sulafat", "Umbriel", "Vindemiatrix", "Zephyr", "Zubenelgenubi",
        Default = "Kore")]
    public required string Voice { get; init; }

    [JsonPropertyName("inputText")]
    [ConfigurableField(Label = "Input Text", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "The text to convert to speech. Use {{Steps.node_name.Property}} for variables.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string InputText { get; init; }

    [JsonPropertyName("instructions")]
    [ConfigurableField(Label = "Voice Instructions", ControlType = ControlType.TextArea, Order = 20,
        Description = "Guide the voice style via natural language, e.g. 'Speak cheerfully with moderate pacing'.",
        Placeholder = "e.g. Speak in a warm, conversational tone.")]
    [Tab("Content", Order = 2)]
    [SupportVariables]
    public string? Instructions { get; init; }

    [JsonPropertyName("responseFormat")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [SelectOptions("mp3", "wav", "pcm", Default = "mp3")]
    public string ResponseFormat { get; init; } = "mp3";
}
