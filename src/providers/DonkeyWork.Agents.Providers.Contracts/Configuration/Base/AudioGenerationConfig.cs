using DonkeyWork.Agents.Providers.Contracts.Attributes;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Base configuration for audio generation (TTS) models.
/// </summary>
public class AudioGenerationConfig : IModelConfig
{
    [ConfigField(Label = "Voice", Description = "The voice to use for audio generation", Order = 10)]
    [Select(DefaultValue = "alloy")]
    public string? Voice { get; init; }

    [ConfigField(Label = "Speed", Description = "The speed of the generated audio (0.25 to 4.0)", Order = 20)]
    [Slider(Min = 0.25, Max = 4.0, Step = 0.25, DefaultValue = 1.0)]
    public double? Speed { get; init; }
}
