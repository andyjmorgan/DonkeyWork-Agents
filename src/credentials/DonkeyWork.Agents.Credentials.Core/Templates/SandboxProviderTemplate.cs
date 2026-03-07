using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Core.Templates;

/// <summary>
/// Defines a known provider's sandbox credential mapping template.
/// </summary>
public sealed class SandboxProviderTemplate
{
    public required OAuthProvider Provider { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<SandboxProviderMappingTemplate> Mappings { get; init; }
}
