namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Status of an OAuth token.
/// </summary>
public enum OAuthTokenStatus
{
    /// <summary>
    /// Token is active and valid.
    /// </summary>
    Active,

    /// <summary>
    /// Token is expiring soon (within 10 minutes).
    /// </summary>
    ExpiringSoon,

    /// <summary>
    /// Token has expired.
    /// </summary>
    Expired
}
