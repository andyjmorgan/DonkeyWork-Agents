using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

/// <summary>
/// Quality settings for image generation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageQuality
{
    Standard,
    High
}
