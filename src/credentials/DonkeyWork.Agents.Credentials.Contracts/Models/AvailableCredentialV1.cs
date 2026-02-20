namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Represents an available credential type with its configuration status.
/// </summary>
public sealed class AvailableCredentialV1
{
    /// <summary>
    /// The type of credential (e.g., "OAuth" or "ApiKey").
    /// </summary>
    public required string CredentialType { get; init; }

    /// <summary>
    /// The provider name (e.g., "Google", "OpenAI").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Whether the user has configured this credential.
    /// </summary>
    public required bool IsConfigured { get; init; }
}
