using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

/// <summary>
/// Standard image size options for image generation models.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageSize
{
    Square1024,
    Landscape1792x1024,
    Portrait1024x1792
}
