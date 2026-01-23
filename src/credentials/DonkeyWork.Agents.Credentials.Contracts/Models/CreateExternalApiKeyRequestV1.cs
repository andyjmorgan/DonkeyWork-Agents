using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Request to create an external API key credential.
/// </summary>
public sealed class CreateExternalApiKeyRequestV1
{
    /// <summary>
    /// The provider this API key is for (e.g., OpenAI, Anthropic, Google).
    /// </summary>
    [Required]
    public required ExternalApiKeyProvider Provider { get; init; }

    /// <summary>
    /// A friendly name for this credential.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Name { get; init; }

    /// <summary>
    /// The API key value.
    /// </summary>
    [Required]
    public required string ApiKey { get; init; }
}
