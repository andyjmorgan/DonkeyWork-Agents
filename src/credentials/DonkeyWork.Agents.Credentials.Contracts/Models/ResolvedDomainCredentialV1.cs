namespace DonkeyWork.Agents.Credentials.Contracts.Models;

public sealed class ResolvedDomainCredentialV1
{
    public required string BaseDomain { get; init; }

    public required Dictionary<string, string> Headers { get; init; }
}
