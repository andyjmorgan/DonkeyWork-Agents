using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Configuration options for OAuth token management.
/// </summary>
public sealed class OAuthOptions
{
    public const string SectionName = "OAuth";

    /// <summary>
    /// How often the background worker checks for expiring tokens.
    /// </summary>
    [Required]
    public TimeSpan TokenRefreshCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Tokens expiring within this window will be refreshed.
    /// </summary>
    [Required]
    public TimeSpan TokenRefreshWindow { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum number of times to retry refreshing a failed token.
    /// </summary>
    [Range(1, 10)]
    public int MaxRefreshRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// </summary>
    [Required]
    public TimeSpan RefreshRetryDelay { get; set; } = TimeSpan.FromMinutes(1);
}
