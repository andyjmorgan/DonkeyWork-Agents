namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Metadata about an OAuth scope, including its name, description,
/// and whether it is required or default.
/// </summary>
public sealed class OAuthScopeMetadataV1
{
    /// <summary>
    /// The scope identifier (e.g. "openid", "User.Read").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the scope grants access to.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this scope is required and cannot be deselected.
    /// </summary>
    public required bool IsRequired { get; init; }

    /// <summary>
    /// Whether this scope is selected by default (can be deselected if not required).
    /// </summary>
    public required bool IsDefault { get; init; }
}
