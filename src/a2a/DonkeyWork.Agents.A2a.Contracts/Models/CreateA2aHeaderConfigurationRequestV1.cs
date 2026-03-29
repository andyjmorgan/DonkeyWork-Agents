using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class CreateA2aHeaderConfigurationRequestV1
{
    [JsonPropertyName("headerName")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string HeaderName { get; init; }

    [JsonPropertyName("headerValue")]
    [StringLength(4000)]
    public string? HeaderValue { get; init; }

    [JsonPropertyName("credentialId")]
    public Guid? CredentialId { get; init; }

    [JsonPropertyName("credentialFieldType")]
    [StringLength(50)]
    public string? CredentialFieldType { get; init; }
}
