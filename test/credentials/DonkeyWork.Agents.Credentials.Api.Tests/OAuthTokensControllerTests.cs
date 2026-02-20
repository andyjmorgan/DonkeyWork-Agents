using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests;

public class OAuthTokensControllerTests
{
    private readonly Mock<IOAuthTokenService> _tokenServiceMock;
    private readonly Mock<IOAuthProviderConfigService> _configServiceMock;
    private readonly Mock<IOAuthProviderFactory> _providerFactoryMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly Mock<ILogger<OAuthTokensController>> _loggerMock;
    private readonly OAuthTokensController _controller;
    private readonly Guid _userId;

    public OAuthTokensControllerTests()
    {
        _tokenServiceMock = new Mock<IOAuthTokenService>();
        _configServiceMock = new Mock<IOAuthProviderConfigService>();
        _providerFactoryMock = new Mock<IOAuthProviderFactory>();
        _identityContextMock = new Mock<IIdentityContext>();
        _loggerMock = new Mock<ILogger<OAuthTokensController>>();
        _userId = Guid.NewGuid();

        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _controller = new OAuthTokensController(
            _tokenServiceMock.Object,
            _configServiceMock.Object,
            _providerFactoryMock.Object,
            _identityContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task List_WithTokens_ReturnsOkWithTokenList()
    {
        // Arrange
        var tokens = new List<OAuthToken>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.Google,
                Email = "user@example.com",
                ExternalUserId = "ext_123",
                AccessToken = "access_token_1",
                RefreshToken = "refresh_token_1",
                Scopes = new[] { "email", "profile" },
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                LastRefreshedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.Microsoft,
                Email = "user@microsoft.com",
                ExternalUserId = "ext_456",
                AccessToken = "access_token_2",
                RefreshToken = "refresh_token_2",
                Scopes = new[] { "User.Read" },
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<OAuthTokenItemV1>>(okResult.Value);
        Assert.Equal(2, items.Count);
        Assert.Equal(OAuthTokenStatus.Active, items[0].Status);
        Assert.Equal(OAuthTokenStatus.ExpiringSoon, items[1].Status);
    }

    [Fact]
    public async Task List_WithExpiredToken_SetsExpiredStatus()
    {
        // Arrange
        var tokens = new List<OAuthToken>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.GitHub,
                Email = "user@github.com",
                ExternalUserId = "ext_789",
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                Scopes = Array.Empty<string>(),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10), // Expired
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<OAuthTokenItemV1>>(okResult.Value);
        Assert.Single(items);
        Assert.Equal(OAuthTokenStatus.Expired, items[0].Status);
    }

    [Fact]
    public async Task Get_WithExistingToken_ReturnsOkWithDetail()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "long_access_token_value_12345",
            RefreshToken = "refresh_token",
            Scopes = new[] { "email", "profile", "openid" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            LastRefreshedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Get(tokenId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<OAuthTokenDetailV1>(okResult.Value);
        Assert.Equal(tokenId, detail.Id);
        Assert.Equal("user@example.com", detail.Email);
        Assert.Contains("...", detail.AccessToken); // Should be masked
        Assert.Equal(3, detail.Scopes.Count);
    }

    [Fact]
    public async Task Get_WithNonExistingToken_ReturnsNotFound()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act
        var result = await _controller.Get(tokenId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOkWithSuccess()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Microsoft,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "old_access_token",
            RefreshToken = "refresh_token",
            Scopes = new[] { "User.Read" },
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = OAuthProvider.Microsoft,
            ClientId = "client_id",
            ClientSecret = "client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();
        var newExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var tokenResponse = new OAuthTokenResponse(
            "new_access_token",
            "new_refresh_token",
            3600,
            "Bearer",
            new[] { "User.Read" });

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _configServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.Microsoft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(OAuthProvider.Microsoft, config))
            .Returns(providerMock.Object);

        providerMock
            .Setup(p => p.RefreshTokenAsync(
                token.RefreshToken,
                config.ClientId,
                config.ClientSecret,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        _tokenServiceMock
            .Setup(s => s.RefreshTokenAsync(
                tokenId,
                "new_access_token",
                "new_refresh_token",
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Refresh(tokenId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RefreshTokenResponseV1>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.ExpiresAt);
    }

    [Fact]
    public async Task Refresh_WithNonExistingToken_ReturnsNotFound()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act
        var result = await _controller.Refresh(tokenId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Refresh_WithMissingProviderConfig_ReturnsBadRequest()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Scopes = Array.Empty<string>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _configServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.Google, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthProviderConfig?)null);

        // Act
        var result = await _controller.Refresh(tokenId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<RefreshTokenResponseV1>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
    }

    [Fact]
    public async Task Refresh_WithProviderException_ReturnsBadRequest()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.GitHub,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Scopes = Array.Empty<string>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var config = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = OAuthProvider.GitHub,
            ClientId = "client_id",
            ClientSecret = "client_secret",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var providerMock = new Mock<IOAuthProvider>();

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _configServiceMock
            .Setup(s => s.GetByProviderAsync(_userId, OAuthProvider.GitHub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _providerFactoryMock
            .Setup(f => f.GetProvider(OAuthProvider.GitHub))
            .Returns(providerMock.Object);

        providerMock
            .Setup(p => p.RefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Token refresh failed"));

        // Act
        var result = await _controller.Refresh(tokenId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<RefreshTokenResponseV1>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Token refresh failed", response.Error);
    }

    [Fact]
    public async Task Delete_WithExistingToken_ReturnsNoContent()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _tokenServiceMock
            .Setup(s => s.DeleteAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(tokenId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonExistingToken_ReturnsNotFound()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _tokenServiceMock
            .Setup(s => s.DeleteAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Token not found"));

        // Act
        var result = await _controller.Delete(tokenId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_MasksAccessToken()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "very_long_access_token_value_that_should_be_masked",
            RefreshToken = "refresh_token",
            Scopes = Array.Empty<string>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Get(tokenId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<OAuthTokenDetailV1>(okResult.Value);
        Assert.NotEqual(token.AccessToken, detail.AccessToken);
        Assert.Contains("...", detail.AccessToken);
    }

    [Fact]
    public async Task List_WithEmptyTokens_ReturnsOkWithEmptyList()
    {
        // Arrange
        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthToken>());

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<OAuthTokenItemV1>>(okResult.Value);
        Assert.Empty(items);
    }

    #region GetAccessToken Tests

    [Fact]
    public async Task GetAccessToken_WithExistingToken_ReturnsOkWithUnmaskedToken()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "ya29.full_unmasked_access_token_value",
            RefreshToken = "refresh_token",
            Scopes = new[] { "email", "profile" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.GetAccessToken(tokenId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetOAuthAccessTokenResponseV1>(okResult.Value);
        Assert.Equal(tokenId, response.Id);
        Assert.Equal("user@example.com", response.Email);
        Assert.Equal("ya29.full_unmasked_access_token_value", response.AccessToken);
        Assert.Equal(OAuthTokenStatus.Active, response.Status);
    }

    [Fact]
    public async Task GetAccessToken_WithNonExistingToken_ReturnsNotFound()
    {
        // Arrange
        var tokenId = Guid.NewGuid();

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act
        var result = await _controller.GetAccessToken(tokenId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAccessToken_DoesNotIncludeRefreshToken()
    {
        // Arrange
        var tokenId = Guid.NewGuid();
        var token = new OAuthToken
        {
            Id = tokenId,
            UserId = _userId,
            Provider = OAuthProvider.Microsoft,
            Email = "user@example.com",
            ExternalUserId = "ext_123",
            AccessToken = "access_token_value",
            RefreshToken = "secret_refresh_token",
            Scopes = new[] { "User.Read" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _tokenServiceMock
            .Setup(s => s.GetByIdAsync(_userId, tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.GetAccessToken(tokenId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetOAuthAccessTokenResponseV1>(okResult.Value);

        // Verify the response type does not have a RefreshToken property
        var properties = typeof(GetOAuthAccessTokenResponseV1).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name == "RefreshToken");
    }

    #endregion
}
