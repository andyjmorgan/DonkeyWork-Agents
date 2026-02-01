namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;

public sealed class TestUser
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;

    public static TestUser Default { get; } = new()
    {
        UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Email = "test@example.com",
        Name = "Test User",
        Username = "testuser"
    };

    public static TestUser CreateRandom()
    {
        var id = Guid.NewGuid();
        var shortId = id.ToString("N")[..8];
        return new TestUser
        {
            UserId = id,
            Email = $"user-{id:N}@example.com",
            Name = $"User {shortId}",
            Username = $"user_{shortId}"
        };
    }
}
