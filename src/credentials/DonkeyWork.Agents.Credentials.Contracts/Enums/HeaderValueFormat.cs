namespace DonkeyWork.Agents.Credentials.Contracts.Enums;

/// <summary>
/// How the credential value should be formatted in the HTTP header.
/// </summary>
public enum HeaderValueFormat
{
    /// <summary>
    /// Use the value as-is with optional prefix: prefix + value.
    /// </summary>
    Raw,

    /// <summary>
    /// Format as HTTP Basic auth: "Basic " + base64(username:value).
    /// </summary>
    BasicAuth,
}
