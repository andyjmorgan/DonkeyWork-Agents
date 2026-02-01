using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Models;
using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Base configuration for audio generation (TTS) models.
/// </summary>
public class AudioGenerationConfig : BaseConfigurableParameters, IModelConfig
{
    [ConfigurableField(Label = "Voice", Description = "The voice to use for audio generation", Order = 10)]
    [SelectOptions(Default = "alloy")]
    public Resolvable<string>? Voice { get; init; }

    [ConfigurableField(Label = "Speed", Description = "The speed of the generated audio (0.25 to 4.0)", Order = 20)]
    [Slider(Min = 0.25, Max = 4.0, Step = 0.25, Default = 1.0)]
    public Resolvable<double>? Speed { get; init; }
}
