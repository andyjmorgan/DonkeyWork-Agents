using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the TextToSpeech node - generates speech audio from text using OpenAI TTS.
/// </summary>
[Node(
    DisplayName = "Text to Speech",
    Description = "Generate speech audio from text using OpenAI TTS",
    Category = "AI",
    Icon = "volume-2",
    Color = "pink")]
public sealed class TextToSpeechNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.TextToSpeech;

    /// <summary>
    /// The credential ID for authenticating with OpenAI.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    /// <summary>
    /// The TTS model to use.
    /// </summary>
    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Select, Order = 20)]
    [Tab("Basic", Order = 1)]
    [SelectOptions("tts-1", "tts-1-hd", Default = "tts-1")]
    public required string Model { get; init; }

    /// <summary>
    /// The voice to use for speech generation.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Select, Order = 30)]
    [Tab("Basic", Order = 1)]
    [SelectOptions("alloy", "ash", "coral", "echo", "fable", "onyx", "nova", "sage", "shimmer", Default = "alloy")]
    public required string Voice { get; init; }

    /// <summary>
    /// The text to convert to speech. Supports Scriban template variables.
    /// </summary>
    [JsonPropertyName("inputText")]
    [ConfigurableField(Label = "Input Text", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "The text to convert to speech. Use {{Steps.node_name.Property}} for variables.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string InputText { get; init; }

    /// <summary>
    /// The speed of the generated audio (0.25 to 4.0).
    /// </summary>
    [JsonPropertyName("speed")]
    [ConfigurableField(Label = "Speed", ControlType = ControlType.Slider, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [Slider(Min = 0.25, Max = 4.0, Step = 0.25, Default = 1.0)]
    public double Speed { get; init; } = 1.0;

    /// <summary>
    /// The audio output format.
    /// </summary>
    [JsonPropertyName("responseFormat")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 20)]
    [Tab("Advanced", Order = 3)]
    [SelectOptions("mp3", "opus", "aac", "flac", "wav", Default = "mp3")]
    public string ResponseFormat { get; init; } = "mp3";
}
