using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class CreateApiKeyRequestV1
{
    [Required]
    [MaxLength(255)]
    public required string Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }
}
