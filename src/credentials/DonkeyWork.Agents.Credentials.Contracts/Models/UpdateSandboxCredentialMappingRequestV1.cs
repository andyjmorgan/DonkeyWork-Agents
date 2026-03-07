using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class UpdateSandboxCredentialMappingRequestV1
{
    public string? HeaderName { get; set; }

    public string? HeaderValuePrefix { get; set; }

    public Guid? CredentialId { get; set; }

    public string? CredentialType { get; set; }

    public CredentialFieldType? CredentialFieldType { get; set; }

    public HeaderValueFormat? HeaderValueFormat { get; set; }

    public string? BasicAuthUsername { get; set; }
}
