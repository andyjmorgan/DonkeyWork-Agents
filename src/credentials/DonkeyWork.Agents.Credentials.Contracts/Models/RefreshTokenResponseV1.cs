namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Response from token refresh operation.
/// </summary>
public sealed class RefreshTokenResponseV1
{
    /// <summary>
    /// Whether the refresh was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// New expiration time (if successful).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? Error { get; init; }
}
