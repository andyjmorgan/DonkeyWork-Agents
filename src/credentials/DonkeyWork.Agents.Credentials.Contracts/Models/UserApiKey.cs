namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Represents an internal API key created by a user for accessing their own data.
/// </summary>
public sealed class UserApiKey
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The API key value. May be masked (dk_abc***xyz) or full depending on context.
    /// </summary>
    public required string Key { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
