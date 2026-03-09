using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Services;

public class SandboxCredentialMappingServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly Mock<IExternalApiKeyService> _externalApiKeyServiceMock;
    private readonly Mock<IOAuthTokenService> _oAuthTokenServiceMock;
    private readonly Mock<ILogger<SandboxCredentialMappingService>> _loggerMock;
    private readonly SandboxCredentialMappingService _service;
    private readonly Guid _userId;

    public SandboxCredentialMappingServiceTests()
    {
        _userId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _identityContextMock = new Mock<IIdentityContext>();
        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _dbContext = new AgentsDbContext(options, _identityContextMock.Object);

        _externalApiKeyServiceMock = new Mock<IExternalApiKeyService>();
        _oAuthTokenServiceMock = new Mock<IOAuthTokenService>();
        _loggerMock = new Mock<ILogger<SandboxCredentialMappingService>>();

        _service = new SandboxCredentialMappingService(
            _dbContext,
            _identityContextMock.Object,
            _externalApiKeyServiceMock.Object,
            _oAuthTokenServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private async Task<SandboxCredentialMappingEntity> SeedMappingAsync(
        string baseDomain = "api.example.com",
        string headerName = "Authorization",
        string? headerValuePrefix = null,
        string headerValueFormat = "Raw",
        string? basicAuthUsername = null,
        Guid? credentialId = null,
        string credentialType = "ExternalApiKey",
        CredentialFieldType credentialFieldType = CredentialFieldType.ApiKey,
        Guid? userId = null,
        string? managedByProvider = null)
    {
        var entity = new SandboxCredentialMappingEntity
        {
            UserId = userId ?? _userId,
            BaseDomain = baseDomain,
            HeaderName = headerName,
            HeaderValuePrefix = headerValuePrefix,
            HeaderValueFormat = headerValueFormat,
            BasicAuthUsername = basicAuthUsername,
            CredentialId = credentialId ?? Guid.NewGuid(),
            CredentialType = credentialType,
            CredentialFieldType = credentialFieldType.ToString(),
            ManagedByProvider = managedByProvider,
        };

        _dbContext.SandboxCredentialMappings.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_NoMappings_ReturnsEmptyList()
    {
        // Act
        var result = await _service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_WithMappings_ReturnsMappingsOrderedByDomainThenHeader()
    {
        // Arrange
        await SeedMappingAsync(baseDomain: "z-domain.com", headerName: "X-Api-Key");
        await SeedMappingAsync(baseDomain: "a-domain.com", headerName: "Authorization");
        await SeedMappingAsync(baseDomain: "a-domain.com", headerName: "Api-Key");

        // Act
        var result = await _service.ListAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("a-domain.com", result[0].BaseDomain);
        Assert.Equal("Api-Key", result[0].HeaderName);
        Assert.Equal("a-domain.com", result[1].BaseDomain);
        Assert.Equal("Authorization", result[1].HeaderName);
        Assert.Equal("z-domain.com", result[2].BaseDomain);
    }

    [Fact]
    public async Task ListAsync_WithMultipleUsers_ReturnsOnlyCurrentUserMappings()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        await SeedMappingAsync(baseDomain: "my-domain.com");
        await SeedMappingAsync(baseDomain: "other-domain.com", userId: otherUserId);

        // Act
        var result = await _service.ListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("my-domain.com", result[0].BaseDomain);
    }

    [Fact]
    public async Task ListAsync_ExcludesProviderManagedMappings()
    {
        // Arrange
        await SeedMappingAsync(baseDomain: "api.openai.com", headerName: "Authorization");
        await SeedMappingAsync(baseDomain: "api.github.com", headerName: "Authorization", managedByProvider: "GitHub");
        await SeedMappingAsync(baseDomain: "github.com", headerName: "Authorization", managedByProvider: "GitHub");

        // Act
        var result = await _service.ListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("api.openai.com", result[0].BaseDomain);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingMapping_ReturnsMapping()
    {
        // Arrange
        var entity = await SeedMappingAsync(baseDomain: "test.com");

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("test.com", result.BaseDomain);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesMappingWithCorrectUserId()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var request = new CreateSandboxCredentialMappingRequestV1
        {
            BaseDomain = "api.openai.com",
            HeaderName = "Authorization",
            HeaderValuePrefix = "Bearer ",
            CredentialId = credentialId,
            CredentialType = "ExternalApiKey",
            CredentialFieldType = CredentialFieldType.ApiKey,
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("api.openai.com", result.BaseDomain);
        Assert.Equal("Authorization", result.HeaderName);
        Assert.Equal("Bearer ", result.HeaderValuePrefix);
        Assert.Equal(credentialId, result.CredentialId);
        Assert.Equal("ExternalApiKey", result.CredentialType);
        Assert.Equal(CredentialFieldType.ApiKey, result.CredentialFieldType);

        // Verify entity was persisted with correct user ID
        var entity = await _dbContext.SandboxCredentialMappings.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);
        Assert.Equal(_userId, entity.UserId);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsCredentialFieldTypeAsString()
    {
        // Arrange
        var request = new CreateSandboxCredentialMappingRequestV1
        {
            BaseDomain = "api.example.com",
            HeaderName = "X-Token",
            CredentialId = Guid.NewGuid(),
            CredentialType = "OAuthToken",
            CredentialFieldType = CredentialFieldType.AccessToken,
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        var entity = await _dbContext.SandboxCredentialMappings.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(entity);
        Assert.Equal("AccessToken", entity.CredentialFieldType);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingMapping_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var entity = await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "Authorization",
            headerValuePrefix: "Bearer ");

        var request = new UpdateSandboxCredentialMappingRequestV1
        {
            HeaderName = "X-Api-Key",
        };

        // Act
        var result = await _service.UpdateAsync(entity.Id, request);

        // Assert
        Assert.Equal("X-Api-Key", result.HeaderName);
        Assert.Equal("Bearer ", result.HeaderValuePrefix); // Unchanged
        Assert.Equal("api.example.com", result.BaseDomain); // Unchanged
    }

    [Fact]
    public async Task UpdateAsync_ProviderManagedMapping_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = await SeedMappingAsync(
            baseDomain: "api.github.com",
            headerName: "Authorization",
            managedByProvider: "GitHub");

        var request = new UpdateSandboxCredentialMappingRequestV1
        {
            HeaderName = "X-Api-Key",
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(entity.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new UpdateSandboxCredentialMappingRequestV1
        {
            HeaderName = "X-Api-Key",
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(Guid.NewGuid(), request));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingMapping_RemovesMapping()
    {
        // Arrange
        var entity = await SeedMappingAsync();

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        var result = await _service.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ProviderManagedMapping_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = await SeedMappingAsync(
            baseDomain: "api.github.com",
            headerName: "Authorization",
            managedByProvider: "GitHub");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _service.DeleteAsync(Guid.NewGuid());
    }

    #endregion

    #region ResolveForDomainAsync Tests

    [Fact]
    public async Task ResolveForDomainAsync_NoMappingsForDomain_ReturnsNull()
    {
        // Act
        var result = await _service.ResolveForDomainAsync("unknown.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveForDomainAsync_WithExternalApiKey_ReturnsHeaderWithValue()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "X-Api-Key",
            credentialId: credentialId,
            credentialType: "ExternalApiKey",
            credentialFieldType: CredentialFieldType.ApiKey);

        _externalApiKeyServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiKey
            {
                Id = credentialId,
                UserId = _userId,
                Provider = ExternalApiKeyProvider.OpenAI,
                Name = "Test Key",
                Fields = new Dictionary<CredentialFieldType, string>
                {
                    { CredentialFieldType.ApiKey, "sk-test-key-123" },
                },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ResolveForDomainAsync("api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("api.example.com", result.BaseDomain);
        Assert.Single(result.Headers);
        Assert.Equal("sk-test-key-123", result.Headers["X-Api-Key"]);
    }

    [Fact]
    public async Task ResolveForDomainAsync_WithExternalApiKeyAndPrefix_ReturnsPrefixedValue()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "Authorization",
            headerValuePrefix: "Bearer ",
            credentialId: credentialId,
            credentialType: "ExternalApiKey",
            credentialFieldType: CredentialFieldType.ApiKey);

        _externalApiKeyServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiKey
            {
                Id = credentialId,
                UserId = _userId,
                Provider = ExternalApiKeyProvider.OpenAI,
                Name = "Test Key",
                Fields = new Dictionary<CredentialFieldType, string>
                {
                    { CredentialFieldType.ApiKey, "sk-test-key-123" },
                },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ResolveForDomainAsync("api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bearer sk-test-key-123", result.Headers["Authorization"]);
    }

    [Fact]
    public async Task ResolveForDomainAsync_WithOAuthAccessToken_ReturnsHeaderWithAccessToken()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "graph.microsoft.com",
            headerName: "Authorization",
            headerValuePrefix: "Bearer ",
            credentialId: credentialId,
            credentialType: "OAuthToken",
            credentialFieldType: CredentialFieldType.AccessToken);

        _oAuthTokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = credentialId,
                UserId = _userId,
                Provider = OAuthProvider.Microsoft,
                ExternalUserId = "ext_123",
                Email = "user@microsoft.com",
                AccessToken = "eyJ-access-token",
                RefreshToken = "eyJ-refresh-token",
                Scopes = new List<string> { "User.Read" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ResolveForDomainAsync("graph.microsoft.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bearer eyJ-access-token", result.Headers["Authorization"]);
    }

    [Fact]
    public async Task ResolveForDomainAsync_WithMultipleHeaders_ReturnsAllHeaders()
    {
        // Arrange
        var credentialId1 = Guid.NewGuid();
        var credentialId2 = Guid.NewGuid();

        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "Authorization",
            headerValuePrefix: "Bearer ",
            credentialId: credentialId1,
            credentialType: "ExternalApiKey",
            credentialFieldType: CredentialFieldType.ApiKey);

        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "X-Custom-Header",
            credentialId: credentialId2,
            credentialType: "ExternalApiKey",
            credentialFieldType: CredentialFieldType.ApiKey);

        _externalApiKeyServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiKey
            {
                Id = credentialId1,
                UserId = _userId,
                Provider = ExternalApiKeyProvider.OpenAI,
                Name = "Key 1",
                Fields = new Dictionary<CredentialFieldType, string>
                {
                    { CredentialFieldType.ApiKey, "key-value-1" },
                },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        _externalApiKeyServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalApiKey
            {
                Id = credentialId2,
                UserId = _userId,
                Provider = ExternalApiKeyProvider.OpenAI,
                Name = "Key 2",
                Fields = new Dictionary<CredentialFieldType, string>
                {
                    { CredentialFieldType.ApiKey, "key-value-2" },
                },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ResolveForDomainAsync("api.example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("Bearer key-value-1", result.Headers["Authorization"]);
        Assert.Equal("key-value-2", result.Headers["X-Custom-Header"]);
    }

    [Fact]
    public async Task ResolveForDomainAsync_CredentialNotFound_SkipsHeaderAndReturnsNull()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "Authorization",
            credentialId: credentialId,
            credentialType: "ExternalApiKey",
            credentialFieldType: CredentialFieldType.ApiKey);

        _externalApiKeyServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalApiKey?)null);

        // Act
        var result = await _service.ResolveForDomainAsync("api.example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveForDomainAsync_UnknownCredentialType_SkipsHeader()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "api.example.com",
            headerName: "Authorization",
            credentialId: credentialId,
            credentialType: "UnknownType",
            credentialFieldType: CredentialFieldType.ApiKey);

        // Act
        var result = await _service.ResolveForDomainAsync("api.example.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ResolveForDomainAsync BasicAuth Tests

    [Fact]
    public async Task ResolveForDomainAsync_WithBasicAuthFormat_ReturnsBase64EncodedHeader()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        await SeedMappingAsync(
            baseDomain: "github.com",
            headerName: "Authorization",
            credentialId: credentialId,
            credentialType: "OAuthToken",
            credentialFieldType: CredentialFieldType.AccessToken,
            headerValueFormat: "BasicAuth",
            basicAuthUsername: "x-access-token");

        _oAuthTokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = credentialId,
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                ExternalUserId = "ext_123",
                Email = "user@github.com",
                AccessToken = "test-token",
                RefreshToken = "refresh-token",
                Scopes = new List<string> { "repo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ResolveForDomainAsync("github.com");

        // Assert
        Assert.NotNull(result);
        var expectedValue = "Basic " + Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("x-access-token:test-token"));
        Assert.Equal(expectedValue, result.Headers["Authorization"]);
    }

    #endregion

    #region CreateFromProviderAsync Tests

    [Fact]
    public async Task CreateFromProviderAsync_WithGitHub_CreatesBothMappings()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = tokenId,
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                ExternalUserId = "ext_123",
                Email = "user@github.com",
                AccessToken = "test-token",
                RefreshToken = "refresh-token",
                Scopes = new List<string> { "repo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.CreateFromProviderAsync(OAuthProvider.GitHub);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.BaseDomain == "api.github.com" && m.HeaderValueFormat == HeaderValueFormat.Raw);
        Assert.Contains(result, m => m.BaseDomain == "github.com" && m.HeaderValueFormat == HeaderValueFormat.BasicAuth);
    }

    [Fact]
    public async Task CreateFromProviderAsync_SetsManagedByProviderField()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = tokenId,
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                ExternalUserId = "ext_123",
                Email = "user@github.com",
                AccessToken = "test-token",
                RefreshToken = "refresh-token",
                Scopes = new List<string> { "repo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.CreateFromProviderAsync(OAuthProvider.GitHub);

        // Assert
        Assert.All(result, m => Assert.Equal("GitHub", m.ManagedByProvider));

        // Verify entities in DB
        var entities = await _dbContext.SandboxCredentialMappings.ToListAsync();
        Assert.All(entities, e => Assert.Equal("GitHub", e.ManagedByProvider));
    }

    [Fact]
    public async Task CreateFromProviderAsync_WithNoToken_ThrowsInvalidOperation()
    {
        // Arrange
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFromProviderAsync(OAuthProvider.GitHub));
    }

    [Fact]
    public async Task CreateFromProviderAsync_WithUnknownProvider_ThrowsInvalidOperation()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateFromProviderAsync(OAuthProvider.Google));
    }

    #endregion

    #region DeleteByProviderAsync Tests

    [Fact]
    public async Task DeleteByProviderAsync_RemovesProviderMappings()
    {
        // Arrange
        await SeedMappingAsync(baseDomain: "api.github.com", headerName: "Authorization");
        await SeedMappingAsync(baseDomain: "github.com", headerName: "Authorization");

        // Act
        await _service.DeleteByProviderAsync(OAuthProvider.GitHub);

        // Assert
        var remaining = await _service.ListAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task DeleteByProviderAsync_DoesNotRemoveOtherMappings()
    {
        // Arrange
        await SeedMappingAsync(baseDomain: "api.github.com", headerName: "Authorization");
        await SeedMappingAsync(baseDomain: "api.openai.com", headerName: "Authorization");

        // Act
        await _service.DeleteByProviderAsync(OAuthProvider.GitHub);

        // Assert
        var remaining = await _service.ListAsync();
        Assert.Single(remaining);
        Assert.Equal("api.openai.com", remaining[0].BaseDomain);
    }

    #endregion

    #region ListProviderStatusesAsync Tests

    [Fact]
    public async Task ListProviderStatusesAsync_WithTokenAndMappings_ReturnsEnabled()
    {
        // Arrange
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                ExternalUserId = "ext_123",
                Email = "user@github.com",
                AccessToken = "test-token",
                RefreshToken = "refresh-token",
                Scopes = new List<string> { "repo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        await SeedMappingAsync(baseDomain: "api.github.com", headerName: "Authorization");
        await SeedMappingAsync(baseDomain: "github.com", headerName: "Authorization");

        // Act
        var result = await _service.ListProviderStatusesAsync();

        // Assert
        var github = result.First(s => s.Provider == OAuthProvider.GitHub);
        Assert.True(github.HasOAuthToken);
        Assert.True(github.IsEnabled);
    }

    [Fact]
    public async Task ListProviderStatusesAsync_WithTokenButNoMappings_ReturnsNotEnabled()
    {
        // Arrange
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                ExternalUserId = "ext_123",
                Email = "user@github.com",
                AccessToken = "test-token",
                RefreshToken = "refresh-token",
                Scopes = new List<string> { "repo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        // Act
        var result = await _service.ListProviderStatusesAsync();

        // Assert
        var github = result.First(s => s.Provider == OAuthProvider.GitHub);
        Assert.True(github.HasOAuthToken);
        Assert.False(github.IsEnabled);
    }

    [Fact]
    public async Task ListProviderStatusesAsync_WithNoToken_ReturnsNotConnected()
    {
        // Arrange
        _oAuthTokenServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act
        var result = await _service.ListProviderStatusesAsync();

        // Assert
        var github = result.First(s => s.Provider == OAuthProvider.GitHub);
        Assert.False(github.HasOAuthToken);
        Assert.False(github.IsEnabled);
    }

    #endregion

    #region GetConfiguredDomainsAsync Tests

    [Fact]
    public async Task GetConfiguredDomainsAsync_NoMappings_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetConfiguredDomainsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfiguredDomainsAsync_WithMappings_ReturnsDistinctDomains()
    {
        // Arrange
        await SeedMappingAsync(baseDomain: "api.example.com", headerName: "Header1");
        await SeedMappingAsync(baseDomain: "api.example.com", headerName: "Header2");
        await SeedMappingAsync(baseDomain: "graph.microsoft.com", headerName: "Authorization");

        // Act
        var result = await _service.GetConfiguredDomainsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("api.example.com", result);
        Assert.Contains("graph.microsoft.com", result);
    }

    #endregion
}
