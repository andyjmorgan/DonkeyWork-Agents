namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Marker interface for node configurations that require a credential.
/// Used during agent hydration to discover all required credentials.
/// </summary>
public interface IRequiresCredential
{
    /// <summary>
    /// The ID of the credential required by this node.
    /// </summary>
    Guid CredentialId { get; }
}
