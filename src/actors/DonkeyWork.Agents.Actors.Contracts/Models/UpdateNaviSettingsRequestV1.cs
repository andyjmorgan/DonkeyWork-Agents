using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actors.Contracts.Models;

public sealed class UpdateNaviSettingsRequestV1
{
    [JsonPropertyName("modelId")]
    [Required]
    [StringLength(600, MinimumLength = 1)]
    public required string ModelId { get; init; }
}
