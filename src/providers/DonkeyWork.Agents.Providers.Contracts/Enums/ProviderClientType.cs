using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Enums;

/// <summary>
/// Defines the type of provider client based on input/output modalities.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderClientType
{
    /// <summary>
    /// Supports image, text, and audio input with text output.
    /// </summary>
    MultimodalInput,

    /// <summary>
    /// Supports all input types (image, text, audio) with image and text output.
    /// Similar to Gemini's capabilities.
    /// </summary>
    MultimodalDuplex,

    /// <summary>
    /// Supports image generation/output.
    /// </summary>
    ImageOutput,

    /// <summary>
    /// Supports audio generation/output.
    /// </summary>
    AudioOutput,

    /// <summary>
    /// Supports video generation/output.
    /// </summary>
    VideoOutput
}
