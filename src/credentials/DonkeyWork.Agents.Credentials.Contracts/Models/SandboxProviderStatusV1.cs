using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Status of a known provider template for sandbox credential mappings.
/// </summary>
public sealed class SandboxProviderStatusV1
{
    public required OAuthProvider Provider { get; init; }
    public required string DisplayName { get; init; }
    public required bool HasOAuthToken { get; init; }
    public required bool IsEnabled { get; init; }
    public required IReadOnlyList<string> Domains { get; init; }
}
