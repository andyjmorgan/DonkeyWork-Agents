namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class SandboxCredentialMappingEntity : BaseEntity
{
    public string BaseDomain { get; set; } = string.Empty;

    public string HeaderName { get; set; } = string.Empty;

    public string? HeaderValuePrefix { get; set; }

    public Guid CredentialId { get; set; }

    public string CredentialType { get; set; } = string.Empty;

    public string CredentialFieldType { get; set; } = string.Empty;
}
