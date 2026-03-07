using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Core.Templates;

/// <summary>
/// Defines a single domain-to-header mapping within a provider template.
/// </summary>
public sealed class SandboxProviderMappingTemplate
{
    public required string BaseDomain { get; init; }
    public required string HeaderName { get; init; }
    public required HeaderValueFormat HeaderValueFormat { get; init; }
    public string? HeaderValuePrefix { get; init; }
    public string? BasicAuthUsername { get; init; }
    public required CredentialFieldType CredentialFieldType { get; init; }
}
