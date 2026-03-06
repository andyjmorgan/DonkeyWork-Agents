using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class SandboxCredentialMappingV1
{
    public required Guid Id { get; init; }

    public required string BaseDomain { get; init; }

    public required string HeaderName { get; init; }

    public string? HeaderValuePrefix { get; init; }

    public required Guid CredentialId { get; init; }

    public required string CredentialType { get; init; }

    public required CredentialFieldType CredentialFieldType { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
