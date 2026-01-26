using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Base configuration for image generation models.
/// </summary>
public class ImageGenerationConfig : IModelConfig
{
    [ConfigField(Label = "Image Size", Description = "The dimensions of the generated image", Order = 10)]
    [Select(DefaultValue = nameof(ImageSize.Square1024))]
    public ImageSize? Size { get; init; }

    [ConfigField(Label = "Image Quality", Description = "Quality level of the generated image", Order = 20)]
    [Select(DefaultValue = nameof(Enums.ImageQuality.Standard))]
    public ImageQuality? Quality { get; init; }

    [ConfigField(Label = "Number of Images", Description = "Number of images to generate", Order = 30)]
    [RangeConstraint(Min = 1, Max = 4, DefaultValue = 1)]
    public int? NumberOfImages { get; init; }
}
