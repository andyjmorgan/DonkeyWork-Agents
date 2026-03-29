using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aHeaderConfigurationV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("headerName")]
    public required string HeaderName { get; init; }

    [JsonPropertyName("headerValue")]
    public string? HeaderValue { get; init; }

    [JsonPropertyName("isCredentialReference")]
    public bool IsCredentialReference { get; init; }

    [JsonPropertyName("credentialId")]
    public Guid? CredentialId { get; init; }

    [JsonPropertyName("credentialFieldType")]
    public string? CredentialFieldType { get; init; }
}
