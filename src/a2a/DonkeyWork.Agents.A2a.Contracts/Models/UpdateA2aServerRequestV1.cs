using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class UpdateA2aServerRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    [StringLength(2000)]
    public string? Description { get; init; }

    [JsonPropertyName("address")]
    [Required]
    [StringLength(2048, MinimumLength = 1)]
    public required string Address { get; init; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; } = true;

    [JsonPropertyName("connectToNavi")]
    public bool ConnectToNavi { get; init; }

    [JsonPropertyName("publishToMcp")]
    public bool PublishToMcp { get; init; }

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; init; }

    [JsonPropertyName("headerConfigurations")]
    public List<UpdateA2aHeaderConfigurationRequestV1>? HeaderConfigurations { get; init; }
}
