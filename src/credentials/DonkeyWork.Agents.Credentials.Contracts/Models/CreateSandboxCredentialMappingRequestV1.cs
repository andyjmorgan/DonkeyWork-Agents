using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class CreateSandboxCredentialMappingRequestV1
{
    [Required]
    public string BaseDomain { get; set; } = string.Empty;

    [Required]
    public string HeaderName { get; set; } = string.Empty;

    public string? HeaderValuePrefix { get; set; }

    [Required]
    public Guid CredentialId { get; set; }

    [Required]
    public string CredentialType { get; set; } = string.Empty;

    [Required]
    public CredentialFieldType CredentialFieldType { get; set; }
}
