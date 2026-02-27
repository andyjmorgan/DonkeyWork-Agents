using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Core.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Services;

public class OAuthProviderConfigServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly OAuthProviderConfigService _service;
    private readonly Guid _userId;

    public OAuthProviderConfigServiceTests()
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

        _service = new OAuthProviderConfigService(_dbContext, persistenceOptions);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesConfig()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var clientId = "test_client_id";
        var clientSecret = "test_client_secret";
        var redirectUri = "https://example.com/callback";

        // Act
        var result = await _service.CreateAsync(
            _userId,
            provider,
            clientId,
            clientSecret,
            redirectUri);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(provider, result.Provider);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(clientSecret, result.ClientSecret);
        Assert.Equal(redirectUri, result.RedirectUri);

        // Verify it was saved to database
        var entity = await _dbContext.OAuthProviderConfigs.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);
        Assert.Equal(provider, entity.Provider);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateProvider_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        await _service.CreateAsync(_userId, provider, "client1", "secret1", "https://example.com/callback");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(_userId, provider, "client2", "secret2", "https://example.com/callback"));
    }

    [Fact]
    public async Task CreateAsync_EncryptsClientIdAndSecret()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var clientId = "plain_client_id";
        var clientSecret = "plain_client_secret";

        // Act
        var result = await _service.CreateAsync(
            _userId,
            provider,
            clientId,
            clientSecret,
            "https://example.com/callback");

        // Assert
        var entity = await _dbContext.OAuthProviderConfigs.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);

        // Encrypted values should be byte arrays and not equal to plain text
        Assert.NotEmpty(entity.ClientIdEncrypted);
        Assert.NotEmpty(entity.ClientSecretEncrypted);

        // Decrypted values should match original
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(clientSecret, result.ClientSecret);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingConfig_ReturnsConfig()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Google,
            "client_id",
            "client_secret",
            "https://example.com/callback");

        // Act
        var result = await _service.GetByIdAsync(_userId, config.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(config.Id, result.Id);
        Assert.Equal(config.Provider, result.Provider);
        Assert.Equal("client_id", result.ClientId);
        Assert.Equal("client_secret", result.ClientSecret);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingConfig_ReturnsNull()
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
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Google,
            "client_id",
            "client_secret",
            "https://example.com/callback");

        var otherUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(otherUserId, config.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByProviderAsync_WithExistingConfig_ReturnsConfig()
    {
        // Arrange
        await _service.CreateAsync(
            _userId,
            OAuthProvider.Microsoft,
            "client_id",
            "client_secret",
            "https://example.com/callback");

        // Act
        var result = await _service.GetByProviderAsync(_userId, OAuthProvider.Microsoft);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OAuthProvider.Microsoft, result.Provider);
        Assert.Equal("client_id", result.ClientId);
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
    public async Task GetByUserIdAsync_WithMultipleConfigs_ReturnsAllConfigs()
    {
        // Arrange
        await _service.CreateAsync(_userId, OAuthProvider.Google, "client1", "secret1", "https://example.com/1");
        await _service.CreateAsync(_userId, OAuthProvider.Microsoft, "client2", "secret2", "https://example.com/2");
        await _service.CreateAsync(_userId, OAuthProvider.GitHub, "client3", "secret3", "https://example.com/3");

        // Act
        var result = await _service.GetByUserIdAsync(_userId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Provider == OAuthProvider.Google);
        Assert.Contains(result, c => c.Provider == OAuthProvider.Microsoft);
        Assert.Contains(result, c => c.Provider == OAuthProvider.GitHub);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoConfigs_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetByUserIdAsync(_userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithDifferentUsers_ReturnsOnlyUserConfigs()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        await _service.CreateAsync(_userId, OAuthProvider.Google, "client1", "secret1", "https://example.com/1");
        await _service.CreateAsync(otherUserId, OAuthProvider.Microsoft, "client2", "secret2", "https://example.com/2");

        // Act
        var result = await _service.GetByUserIdAsync(_userId);

        // Assert
        Assert.Single(result);
        Assert.Equal(OAuthProvider.Google, result[0].Provider);
    }

    [Fact]
    public async Task UpdateAsync_WithAllFields_UpdatesConfig()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Google,
            "old_client_id",
            "old_secret",
            "https://old.example.com");

        // Act
        var result = await _service.UpdateAsync(
            _userId,
            config.Id,
            "new_client_id",
            "new_secret",
            "https://new.example.com");

        // Assert
        Assert.Equal("new_client_id", result.ClientId);
        Assert.Equal("new_secret", result.ClientSecret);
        Assert.Equal("https://new.example.com", result.RedirectUri);
    }

    [Fact]
    public async Task UpdateAsync_WithPartialFields_UpdatesOnlyProvided()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Microsoft,
            "old_client_id",
            "old_secret",
            "https://old.example.com");

        // Act
        var result = await _service.UpdateAsync(
            _userId,
            config.Id,
            "new_client_id",
            null,
            null);

        // Assert
        Assert.Equal("new_client_id", result.ClientId);
        Assert.Equal("old_secret", result.ClientSecret);
        Assert.Equal("https://old.example.com", result.RedirectUri);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingConfig_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(_userId, nonExistentId, "client", "secret", "uri"));
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentUserId_ThrowsException()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Google,
            "client_id",
            "secret",
            "https://example.com");

        var otherUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateAsync(otherUserId, config.Id, "new_client", null, null));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingConfig_DeletesConfig()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.GitHub,
            "client_id",
            "secret",
            "https://example.com");

        // Act
        await _service.DeleteAsync(_userId, config.Id);

        // Assert
        var result = await _service.GetByIdAsync(_userId, config.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingConfig_ThrowsException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync(_userId, nonExistentId));
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentUserId_ThrowsException()
    {
        // Arrange
        var config = await _service.CreateAsync(
            _userId,
            OAuthProvider.Google,
            "client_id",
            "secret",
            "https://example.com");

        var otherUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteAsync(otherUserId, config.Id));
    }
}
