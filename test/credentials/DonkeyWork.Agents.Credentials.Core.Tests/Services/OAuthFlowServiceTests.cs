using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Services;

public class OAuthFlowServiceTests
{
    private readonly Mock<IOAuthProviderConfigService> _providerConfigServiceMock;
    private readonly Mock<IOAuthTokenService> _tokenServiceMock;
    private readonly Mock<IOAuthProviderFactory> _providerFactoryMock;
    private readonly OAuthFlowService _service;
    private readonly Guid _userId;

    public OAuthFlowServiceTests()
    {
        _providerConfigServiceMock = new Mock<IOAuthProviderConfigService>();
        _tokenServiceMock = new Mock<IOAuthTokenService>();
        _providerFactoryMock = new Mock<IOAuthProviderFactory>();
        _userId = Guid.NewGuid();

        _service = new OAuthFlowService(
            _providerConfigServiceMock.Object,
            _tokenServiceMock.Object,
            _providerFactoryMock.Object);
    }

    [Fact]
    public async Task GenerateAuthorizationUrlAsync_WithValidProvider_ReturnsUrlStateAndVerifier()
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
            .Returns("https://accounts.google.com/o/oauth2/v2/auth?client_id=test&redirect_uri=https%3A%2F%2Fexample.com%2Fcallback&response_type=code&code_challenge=challenge&code_challenge_method=S256&state=state");

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        // Act
        var result = await _service.GenerateAuthorizationUrlAsync(_userId, provider);

        // Assert
        Assert.NotEmpty(result.AuthorizationUrl);
        Assert.NotEmpty(result.State);
        Assert.NotEmpty(result.CodeVerifier);
        Assert.Contains("https://accounts.google.com", result.AuthorizationUrl);

        // Verify provider config was retrieved
        _providerConfigServiceMock.Verify(
            s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify authorization URL was built
        providerMock.Verify(
            p => p.BuildAuthorizationUrl(
                config.ClientId,
                config.RedirectUri,
                It.IsAny<string>(),
                result.State),
            Times.Once);
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
    public async Task GenerateAuthorizationUrlAsync_GeneratesUniquePkceParameters()
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
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        // Act - Generate twice
        var result1 = await _service.GenerateAuthorizationUrlAsync(_userId, provider);
        var result2 = await _service.GenerateAuthorizationUrlAsync(_userId, provider);

        // Assert - Verify parameters are different each time
        Assert.NotEqual(result1.State, result2.State);
        Assert.NotEqual(result1.CodeVerifier, result2.CodeVerifier);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithValidCode_ExchangesAndStoresToken()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_value";
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
                code,
                codeVerifier,
                config.ClientId,
                config.ClientSecret,
                config.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                tokenResponse.AccessToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken!,
                tokenResponse.Scopes,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(storedToken.Id, result.Id);
        Assert.Equal(userInfo.Email, result.Email);
        Assert.Equal(userInfo.ExternalUserId, result.ExternalUserId);

        // Verify code was exchanged for tokens
        providerMock.Verify(
            p => p.ExchangeCodeForTokensAsync(
                code,
                codeVerifier,
                config.ClientId,
                config.ClientSecret,
                config.RedirectUri,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify user info was fetched
        providerMock.Verify(
            p => p.GetUserInfoAsync(tokenResponse.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify token was stored
        _tokenServiceMock.Verify(
            s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken!,
                tokenResponse.Scopes,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithMissingProviderConfig_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var code = "auth_code_123";
        var state = "state_value";
        var codeVerifier = "code_verifier_value";

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier));

        Assert.Contains("OAuth provider configuration not found", exception.Message);
        Assert.Contains(provider.ToString(), exception.Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithNullRefreshToken_StoresEmptyString()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var code = "auth_code_123";
        var state = "state_value";
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
            null, // No refresh token
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
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                string.Empty, // Empty string for null refresh token
                tokenResponse.Scopes ?? Array.Empty<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier);

        // Assert
        Assert.NotNull(result);

        // Verify empty string was passed for refresh token
        _tokenServiceMock.Verify(
            s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                string.Empty,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithNullScopes_UsesDefaultScopes()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var code = "auth_code_123";
        var state = "state_value";
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
            null); // No scopes returned

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
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        providerMock.Setup(p => p.GetDefaultScopes())
            .Returns(defaultScopes);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken!,
                defaultScopes,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier);

        // Assert
        Assert.NotNull(result);

        // Verify default scopes were used
        _tokenServiceMock.Verify(
            s => s.StoreTokenAsync(
                _userId,
                provider,
                userInfo.ExternalUserId,
                userInfo.Email,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken!,
                defaultScopes,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify GetDefaultScopes was called
        providerMock.Verify(p => p.GetDefaultScopes(), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithTokenExchangeFailure_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "invalid_code";
        var state = "state_value";
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

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                code,
                codeVerifier,
                config.ClientId,
                config.ClientSecret,
                config.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Invalid authorization code"));

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier));

        Assert.Contains("Invalid authorization code", exception.Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_WithUserInfoFailure_ThrowsException()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var code = "auth_code_123";
        var state = "state_value";
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
            new[] { "User.Read" });

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                tokenResponse.AccessToken,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failed to fetch user info"));

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier));

        Assert.Contains("Failed to fetch user info", exception.Message);
    }

    [Fact]
    public async Task HandleCallbackAsync_CalculatesCorrectExpirationTime()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_value";
        var codeVerifier = "code_verifier_value";
        var expiresIn = 7200; // 2 hours
        var beforeCall = DateTimeOffset.UtcNow;

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
            expiresIn,
            "Bearer",
            new[] { "email" });

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
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        providerMock.Setup(p => p.ExchangeCodeForTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        providerMock.Setup(p => p.GetUserInfoAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        _providerConfigServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(provider))
            .Returns(providerMock.Object);

        DateTimeOffset? capturedExpiresAt = null;
        _tokenServiceMock
            .Setup(s => s.StoreTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<OAuthProvider>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, OAuthProvider, string, string, string, string, IEnumerable<string>, DateTimeOffset, CancellationToken>(
                (_, _, _, _, _, _, _, expiresAt, _) => capturedExpiresAt = expiresAt)
            .ReturnsAsync(storedToken);

        // Act
        await _service.HandleCallbackAsync(_userId, provider, code, state, codeVerifier);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(capturedExpiresAt);

        var expectedExpiresAt = beforeCall.AddSeconds(expiresIn);
        var expectedExpiresAtMax = afterCall.AddSeconds(expiresIn);

        // Verify expiration is approximately 2 hours from now (within test execution window)
        Assert.True(capturedExpiresAt >= expectedExpiresAt);
        Assert.True(capturedExpiresAt <= expectedExpiresAtMax);
    }
}
