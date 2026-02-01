using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Models;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Base configuration for image generation models.
/// </summary>
public class ImageGenerationConfig : BaseConfigurableParameters, IModelConfig
{
    [ConfigurableField(Label = "Image Size", Description = "The dimensions of the generated image", Order = 10)]
    [SelectOptions(Default = nameof(ImageSize.Square1024))]
    public Resolvable<ImageSize>? Size { get; init; }

    [ConfigurableField(Label = "Image Quality", Description = "Quality level of the generated image", Order = 20)]
    [SelectOptions(Default = nameof(ImageQuality.Standard))]
    public Resolvable<ImageQuality>? Quality { get; init; }

    [ConfigurableField(Label = "Number of Images", Description = "Number of images to generate", Order = 30)]
    [RangeConstraint(Min = 1, Max = 4, Default = 1)]
    public Resolvable<int>? NumberOfImages { get; init; }
}
