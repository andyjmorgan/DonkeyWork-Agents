using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Services;

public class OAuthFlowServiceTests : IDisposable
{
    private readonly Mock<IOAuthProviderConfigService> _providerConfigServiceMock;
    private readonly Mock<IOAuthTokenService> _tokenServiceMock;
    private readonly Mock<IOAuthProviderFactory> _providerFactoryMock;
    private readonly AgentsDbContext _dbContext;
    private readonly OAuthFlowService _service;
    private readonly Guid _userId;

    public OAuthFlowServiceTests()
    {
        _providerConfigServiceMock = new Mock<IOAuthProviderConfigService>();
        _tokenServiceMock = new Mock<IOAuthTokenService>();
        _providerFactoryMock = new Mock<IOAuthProviderFactory>();
        _userId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockIdentityContext = new Mock<IIdentityContext>();
        mockIdentityContext.Setup(x => x.UserId).Returns(_userId);

        _dbContext = new AgentsDbContext(options, mockIdentityContext.Object);

        _service = new OAuthFlowService(
            _providerConfigServiceMock.Object,
            _tokenServiceMock.Object,
            _providerFactoryMock.Object,
            _dbContext);
    }

    public void Dispose() => _dbContext?.Dispose();

    #region GenerateAuthorizationUrlAsync Tests

    [Fact]
    public async Task GenerateAuthorizationUrlAsync_WithValidProvider_ReturnsUrlAndState()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.BuildAuthorizationUrl(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("https://accounts.google.com/o/oauth2/v2/auth?client_id=test");

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        // Act
        var result = await _service.GenerateAuthorizationUrlAsync(_userId, provider);

        // Assert
        Assert.NotEmpty(result.AuthorizationUrl);
        Assert.NotEmpty(result.State);
        Assert.Contains("https://accounts.google.com", result.AuthorizationUrl);
    }

    [Fact]
    public async Task GenerateAuthorizationUrlAsync_StoresStateInDatabase()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.BuildAuthorizationUrl(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("https://accounts.google.com/o/oauth2/v2/auth");

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        // Act
        var result = await _service.GenerateAuthorizationUrlAsync(_userId, provider);

        // Assert
        var storedState = await _dbContext.OAuthStates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.State == result.State);
        Assert.NotNull(storedState);
        Assert.Equal(_userId, storedState.UserId);
        Assert.Equal(provider, storedState.Provider);
        Assert.NotEmpty(storedState.CodeVerifier);
        Assert.True(storedState.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GenerateAuthorizationUrlAsync_WithMissingProviderConfig_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateAuthorizationUrlAsync(_userId, provider));

        Assert.Contains("OAuth provider configuration not found", exception.Message);
        Assert.Contains(provider.ToString(), exception.Message);
    }

    [Fact]
    public async Task GenerateAuthorizationUrlAsync_GeneratesUniqueStates()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.BuildAuthorizationUrl(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns("https://github.com/login/oauth/authorize");

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        // Act - Generate twice
        var result1 = await _service.GenerateAuthorizationUrlAsync(_userId, provider);
        var result2 = await _service.GenerateAuthorizationUrlAsync(_userId, provider);

        // Assert
        Assert.NotEqual(result1.State, result2.State);
    }

    #endregion

    #region ValidateAndConsumeStateAsync Tests

    [Fact]
    public async Task ValidateAndConsumeStateAsync_WithValidState_ReturnsCallbackState()
    {
        // Arrange
        var state = "test_state_value";
        var stateEntity = new OAuthStateEntity
        {
            UserId = _userId,
            State = state,
            Provider = OAuthProvider.Google,
            CodeVerifier = "test_verifier",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _dbContext.OAuthStates.Add(stateEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ValidateAndConsumeStateAsync(state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(OAuthProvider.Google, result.Provider);
        Assert.Equal("test_verifier", result.CodeVerifier);
    }

    [Fact]
    public async Task ValidateAndConsumeStateAsync_ConsumesState()
    {
        // Arrange
        var state = "test_state_value";
        var stateEntity = new OAuthStateEntity
        {
            UserId = _userId,
            State = state,
            Provider = OAuthProvider.Google,
            CodeVerifier = "test_verifier",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _dbContext.OAuthStates.Add(stateEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ValidateAndConsumeStateAsync(state);

        // Assert - State should be deleted
        var remaining = await _dbContext.OAuthStates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.State == state);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task ValidateAndConsumeStateAsync_WithExpiredState_ReturnsNull()
    {
        // Arrange
        var state = "expired_state";
        var stateEntity = new OAuthStateEntity
        {
            UserId = _userId,
            State = state,
            Provider = OAuthProvider.Google,
            CodeVerifier = "test_verifier",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5) // Expired
        };
        _dbContext.OAuthStates.Add(stateEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ValidateAndConsumeStateAsync(state);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndConsumeStateAsync_WithInvalidState_ReturnsNull()
    {
        // Act
        var result = await _service.ValidateAndConsumeStateAsync("nonexistent_state");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAndConsumeStateAsync_CannotBeReused()
    {
        // Arrange
        var state = "one_time_state";
        var stateEntity = new OAuthStateEntity
        {
            UserId = _userId,
            State = state,
            Provider = OAuthProvider.GitHub,
            CodeVerifier = "test_verifier",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _dbContext.OAuthStates.Add(stateEntity);
        await _dbContext.SaveChangesAsync();

        // Act - Use once
        var first = await _service.ValidateAndConsumeStateAsync(state);
        var second = await _service.ValidateAndConsumeStateAsync(state);

        // Assert
        Assert.NotNull(first);
        Assert.Null(second);
    }

    #endregion

    #region HandleCallbackAsync Tests

    [Fact]
    public async Task HandleCallbackAsync_WithValidCode_ExchangesAndStoresToken()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var codeVerifier = "code_verifier_value";

        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var tokenResponse = new OAuthTokenResponse(
            "access_token_value",
            "refresh_token_value",
            3600,
            "Bearer",
            new[] { "email", "profile" });

        var userInfo = new OAuthUserInfo(
            "ext_user_123",
            "user@example.com",
            "Test User");

        var storedToken = new OAuthToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            Email = userInfo.Email,
            ExternalUserId = userInfo.ExternalUserId,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken!,
            Scopes = tokenResponse.Scopes!.ToList(),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                code, codeVerifier, config.ClientId, config.ClientSecret, config.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                tokenResponse.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId, provider, userInfo.ExternalUserId, userInfo.Email,
                tokenResponse.AccessToken, tokenResponse.RefreshToken!,
                tokenResponse.Scopes, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, codeVerifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(storedToken.Id, result.Id);
        Assert.Equal(userInfo.Email, result.Email);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithMissingProviderConfig_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.HandleCallbackAsync(_userId, provider, "code", "verifier"));

        Assert.Contains("OAuth provider configuration not found", exception.Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithNullRefreshToken_StoresEmptyString()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var code = "auth_code_123";
        var codeVerifier = "code_verifier_value";

        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var tokenResponse = new OAuthTokenResponse(
            "access_token_value",
            null,
            3600,
            "Bearer",
            new[] { "user:email" });

        var userInfo = new OAuthUserInfo(
            "ext_user_123",
            "user@github.com",
            "Test User");

        var storedToken = new OAuthToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            Email = userInfo.Email,
            ExternalUserId = userInfo.ExternalUserId,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = string.Empty,
            Scopes = tokenResponse.Scopes!.ToList(),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId, provider, userInfo.ExternalUserId, userInfo.Email,
                tokenResponse.AccessToken, string.Empty,
                tokenResponse.Scopes ?? Array.Empty<string>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, codeVerifier);

        // Assert
        Assert.NotNull(result);
        _tokenServiceMock.Verify(
            s => s.StoreTokenAsync(
                _userId, provider, userInfo.ExternalUserId, userInfo.Email,
                tokenResponse.AccessToken, string.Empty,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithNullScopes_UsesDefaultScopes()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var code = "auth_code_123";
        var codeVerifier = "code_verifier_value";

        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var tokenResponse = new OAuthTokenResponse(
            "access_token_value",
            "refresh_token_value",
            3600,
            "Bearer",
            null);

        var userInfo = new OAuthUserInfo(
            "ext_user_123",
            "user@microsoft.com",
            "Test User");

        var defaultScopes = new[] { "openid", "profile", "email" };

        var storedToken = new OAuthToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            Email = userInfo.Email,
            ExternalUserId = userInfo.ExternalUserId,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken!,
            Scopes = defaultScopes.ToList(),
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        providerMock.Setup(p => p.GetDefaultScopes())
            .Returns(defaultScopes);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId, provider, userInfo.ExternalUserId, userInfo.Email,
                tokenResponse.AccessToken, tokenResponse.RefreshToken!,
                defaultScopes, It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, codeVerifier);

        // Assert
        Assert.NotNull(result);
        providerMock.Verify(p => p.GetDefaultScopes(), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithTokenExchangeFailure_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Invalid authorization code"));

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.HandleCallbackAsync(_userId, provider, "invalid_code", "verifier"));

        Assert.Contains("Invalid authorization code", exception.Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithUserInfoFailure_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var tokenResponse = new OAuthTokenResponse(
            "access_token_value", "refresh_token_value", 3600, "Bearer", new[] { "User.Read" });

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                tokenResponse.AccessToken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failed to fetch user info"));

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider, config))
            .Returns(providerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.HandleCallbackAsync(_userId, provider, "code", "verifier"));

        Assert.Contains("Failed to fetch user info", exception.Message);
    }

    #endregion
}
