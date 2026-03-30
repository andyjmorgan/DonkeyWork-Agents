using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Core.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Services;

public class OAuthTokenServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly OAuthTokenService _service;
    private readonly Guid _userId;

    public OAuthTokenServiceTests()
    {
        _userId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var identityContextMock = new Mock<IIdentityContext>();
        identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _dbContext = new AgentsDbContext(options, identityContextMock.Object);

        var persistenceOptions = Options.Create(new PersistenceOptions
        {
            EncryptionKey = "test-encryption-key-for-unit-tests"
        });

        _service = new OAuthTokenService(_dbContext, persistenceOptions);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task StoreTokenAsync_WithNewToken_CreatesToken()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var externalUserId = "ext_123";
        var email = "user@example.com";
        var accessToken = "access_token_value";
        var refreshToken = "refresh_token_value";
        var scopes = new[] { "email", "profile", "openid" };
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var result = await _service.StoreTokenAsync(
            _userId,
            provider,
            externalUserId,
            email,
            accessToken,
            refreshToken,
            scopes,
            expiresAt);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(provider, result.Provider);
        Assert.Equal(externalUserId, result.ExternalUserId);
        Assert.Equal(email, result.Email);
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(refreshToken, result.RefreshToken);
        Assert.Equal(3, result.Scopes.Count);
        Assert.Equal(expiresAt, result.ExpiresAt);
    }

    [Fact]
    public async Task StoreTokenAsync_WithExistingToken_UpdatesToken()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var externalUserId = "ext_456";
        var email = "user@microsoft.com";

        await _service.StoreTokenAsync(
            _userId,
            provider,
            externalUserId,
            email,
            "old_access_token",
            "old_refresh_token",
            new[] { "User.Read" },
            DateTimeOffset.UtcNow.AddHours(1));

        // Act - store again with same provider
        var result = await _service.StoreTokenAsync(
            _userId,
            provider,
            externalUserId,
            email,
            "new_access_token",
            "new_refresh_token",
            new[] { "User.Read", "Files.ReadWrite" },
            DateTimeOffset.UtcNow.AddHours(2));

        // Assert
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_refresh_token", result.RefreshToken);
        Assert.Equal(2, result.Scopes.Count);

        // Verify only one token exists for this provider
        var tokens = await _service.GetByUserIdAsync(_userId);
        Assert.Single(tokens, t => t.Provider == provider);
    }

    [Fact]
    public async Task StoreTokenAsync_EncryptsTokens()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var accessToken = "plain_access_token";
        var refreshToken = "plain_refresh_token";

        // Act
        var result = await _service.StoreTokenAsync(
            _userId,
            provider,
            "ext_789",
            "user@github.com",
            accessToken,
            refreshToken,
            new[] { "repo" },
            DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        var entity = await _dbContext.OAuthTokens.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);

        // Encrypted values should be byte arrays
        Assert.NotEmpty(entity.AccessTokenEncrypted);
        Assert.NotEmpty(entity.RefreshTokenEncrypted);

        // Decrypted values should match original
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(refreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingToken_ReturnsToken()
    {
        // Arrange
        var token = await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext_123",
            "user@example.com",
            "access_token",
            "refresh_token",
            new[] { "email" },
            DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var result = await _service.GetByIdAsync(_userId, token.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingToken_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(_userId, nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentUserId_ReturnsNull()
    {
        // Arrange
        var token = await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext_123",
            "user@example.com",
            "access_token",
            "refresh_token",
            Array.Empty<string>(),
            DateTimeOffset.UtcNow.AddHours(1));

        var otherUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(otherUserId, token.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleTokens_ReturnsAllTokens()
    {
        // Arrange
        await _service.StoreTokenAsync(_userId, OAuthProvider.Google, "ext1", "user@google.com", "token1", "refresh1", Array.Empty<string>(), DateTimeOffset.UtcNow.AddHours(1));
        await _service.StoreTokenAsync(_userId, OAuthProvider.Microsoft, "ext2", "user@ms.com", "token2", "refresh2", Array.Empty<string>(), DateTimeOffset.UtcNow.AddHours(1));
        await _service.StoreTokenAsync(_userId, OAuthProvider.GitHub, "ext3", "user@gh.com", "token3", "refresh3", Array.Empty<string>(), DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var result = await _service.GetByUserIdAsync(_userId);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoTokens_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetByUserIdAsync(_userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByProviderAsync_WithExistingToken_ReturnsToken()
    {
        // Arrange
        await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Microsoft,
            "ext_123",
            "user@microsoft.com",
            "access_token",
            "refresh_token",
            new[] { "User.Read" },
            DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var result = await _service.GetByProviderAsync(_userId, OAuthProvider.Microsoft);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OAuthProvider.Microsoft, result.Provider);
    }

    [Fact]
    public async Task GetByProviderAsync_WithNonExistingProvider_ReturnsNull()
    {
        // Act
        var result = await _service.GetByProviderAsync(_userId, OAuthProvider.GitHub);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_UpdatesTokenValues()
    {
        // Arrange
        var token = await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext_123",
            "user@example.com",
            "old_access_token",
            "old_refresh_token",
            new[] { "email" },
            DateTimeOffset.UtcNow.AddHours(1));

        var newExpiresAt = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var result = await _service.RefreshTokenAsync(
            token.Id,
            "new_access_token",
            "new_refresh_token",
            newExpiresAt);

        // Assert
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_refresh_token", result.RefreshToken);
        Assert.Equal(newExpiresAt, result.ExpiresAt);
        Assert.NotNull(result.LastRefreshedAt);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNonExistingToken_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RefreshTokenAsync(
                nonExistentId,
                "new_access_token",
                "new_refresh_token",
                DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingToken_DeletesToken()
    {
        // Arrange
        var token = await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.GitHub,
            "ext_123",
            "user@github.com",
            "access_token",
            "refresh_token",
            Array.Empty<string>(),
            DateTimeOffset.UtcNow.AddHours(1));

        // Act
        await _service.DeleteAsync(_userId, token.Id);

        // Assert
        var result = await _service.GetByIdAsync(_userId, token.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingToken_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync(_userId, nonExistentId));
    }

    [Fact]
    public async Task GetExpiringTokensAsync_ReturnsTokensExpiringWithinWindow()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Token expiring in 5 minutes (within window)
        await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext1",
            "user1@example.com",
            "token1",
            "refresh1",
            Array.Empty<string>(),
            now.AddMinutes(5));

        // Token expiring in 15 minutes (outside window)
        await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Microsoft,
            "ext2",
            "user2@example.com",
            "token2",
            "refresh2",
            Array.Empty<string>(),
            now.AddMinutes(15));

        // Token already expired
        await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.GitHub,
            "ext3",
            "user3@example.com",
            "token3",
            "refresh3",
            Array.Empty<string>(),
            now.AddMinutes(-5));

        // Act
        var result = await _service.GetExpiringTokensAsync(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(2, result.Count); // Should include both expiring soon and expired
        Assert.Contains(result, t => t.Provider == OAuthProvider.Google);
        Assert.Contains(result, t => t.Provider == OAuthProvider.GitHub);
        Assert.DoesNotContain(result, t => t.Provider == OAuthProvider.Microsoft);
    }

    [Fact]
    public async Task GetExpiringTokensAsync_WithNoExpiringTokens_ReturnsEmptyList()
    {
        // Arrange
        await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext_123",
            "user@example.com",
            "access_token",
            "refresh_token",
            Array.Empty<string>(),
            DateTimeOffset.UtcNow.AddHours(2));

        // Act
        var result = await _service.GetExpiringTokensAsync(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpiringTokensAsync_ReturnsTokensForAllUsers()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await _service.StoreTokenAsync(user1, OAuthProvider.Google, "ext1", "user1@example.com", "token1", "refresh1", Array.Empty<string>(), now.AddMinutes(5));
        await _service.StoreTokenAsync(user2, OAuthProvider.Microsoft, "ext2", "user2@example.com", "token2", "refresh2", Array.Empty<string>(), now.AddMinutes(5));

        // Act
        var result = await _service.GetExpiringTokensAsync(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task StoreTokenAsync_StoresLastRefreshedAt()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var beforeStore = DateTimeOffset.UtcNow;

        // Act
        var result = await _service.StoreTokenAsync(
            _userId,
            provider,
            "ext_123",
            "user@example.com",
            "access_token",
            "refresh_token",
            Array.Empty<string>(),
            DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        Assert.NotNull(result.LastRefreshedAt);
        Assert.True(result.LastRefreshedAt >= beforeStore);
    }

    [Fact]
    public async Task StoreTokenAsync_StoresScopesAsJson()
    {
        // Arrange
        var scopes = new[] { "email", "profile", "openid" };

        // Act
        var result = await _service.StoreTokenAsync(
            _userId,
            OAuthProvider.Google,
            "ext_123",
            "user@example.com",
            "access_token",
            "refresh_token",
            scopes,
            DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        Assert.Equal(3, result.Scopes.Count);
        Assert.Contains("email", result.Scopes);
        Assert.Contains("profile", result.Scopes);
        Assert.Contains("openid", result.Scopes);

        // Verify JSON storage
        var entity = await _dbContext.OAuthTokens.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);
        Assert.Contains("email", entity.ScopesJson);
    }
}
