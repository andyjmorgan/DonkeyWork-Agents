namespace DonkeyWork.Agents.Credentials.Contracts.Enums;

/// <summary>
/// Known credential field types for external API keys.
/// </summary>
public enum CredentialFieldType
{
    ApiKey,
    Username,
    Password,
    ClientId,
    ClientSecret,
    AccessToken,
    RefreshToken,
    WebhookSecret,
    Custom
}
