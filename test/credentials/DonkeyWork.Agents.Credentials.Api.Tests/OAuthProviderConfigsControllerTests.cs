using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests;

public class OAuthProviderConfigsControllerTests
{
    private readonly Mock<IOAuthProviderConfigService> _configServiceMock;
    private readonly Mock<IOAuthTokenService> _tokenServiceMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly OAuthProviderConfigsController _controller;
    private readonly Guid _userId;

    public OAuthProviderConfigsControllerTests()
    {
        _configServiceMock = new Mock<IOAuthProviderConfigService>();
        _tokenServiceMock = new Mock<IOAuthTokenService>();
        _identityContextMock = new Mock<IIdentityContext>();
        _userId = Guid.NewGuid();

        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _controller = new OAuthProviderConfigsController(
            _configServiceMock.Object,
            _tokenServiceMock.Object,
            _identityContextMock.Object);
    }

    [Fact]
    public async Task List_WithConfigs_ReturnsOkWithConfigList()
    {
        // Arrange
        var configs = new List<OAuthProviderConfig>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.Google,
                ClientId = "client_id_1",
                ClientSecret = "client_secret_1",
                RedirectUri = "https://example.com/callback",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.Microsoft,
                ClientId = "client_id_2",
                ClientSecret = "client_secret_2",
                RedirectUri = "https://example.com/callback",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var tokens = new List<OAuthToken>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                Provider = OAuthProvider.Google,
                ExternalUserId = "ext_123",
                Email = "user@example.com",
                AccessToken = "token",
                RefreshToken = "refresh",
                Scopes = Array.Empty<string>(),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _configServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<OAuthProviderConfigItemV1>>(okResult.Value);
        Assert.Equal(2, items.Count);
        Assert.True(items.First(i => i.Provider == OAuthProvider.Google).HasToken);
        Assert.False(items.First(i => i.Provider == OAuthProvider.Microsoft).HasToken);
    }

    [Fact]
    public async Task List_WithEmptyConfigs_ReturnsOkWithEmptyList()
    {
        // Arrange
        _configServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthProviderConfig>());

        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthToken>());

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<OAuthProviderConfigItemV1>>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Get_WithExistingConfig_ReturnsOkWithDetail()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new OAuthProviderConfig
        {
            Id = configId,
            UserId = _userId,
            Provider = OAuthProvider.GitHub,
            ClientId = "long_client_id_12345",
            ClientSecret = "long_client_secret_67890",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configServiceMock
            .Setup(s => s.GetByIdAsync(_userId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.Get(configId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<OAuthProviderConfigDetailV1>(okResult.Value);
        Assert.Equal(configId, detail.Id);
        Assert.Equal(OAuthProvider.GitHub, detail.Provider);
        Assert.StartsWith("long", detail.ClientId);
        Assert.Contains("***", detail.ClientId);
        Assert.Contains("***", detail.ClientSecret);
    }

    [Fact]
    public async Task Get_WithNonExistingConfig_ReturnsNotFound()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _configServiceMock
            .Setup(s => s.GetByIdAsync(_userId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthProviderConfig?)null);

        // Act
        var result = await _controller.Get(configId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateOAuthProviderConfigRequestV1
        {
            Provider = OAuthProvider.Google,
            ClientId = "new_client_id",
            ClientSecret = "new_client_secret",
            RedirectUri = "https://example.com/callback"
        };

        var createdConfig = new OAuthProviderConfig
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = request.Provider,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            RedirectUri = request.RedirectUri,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configServiceMock
            .Setup(s => s.CreateAsync(
                _userId,
                request.Provider,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConfig);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var item = Assert.IsType<OAuthProviderConfigItemV1>(createdResult.Value);
        Assert.Equal(createdConfig.Id, item.Id);
        Assert.Equal(request.Provider, item.Provider);
        Assert.Equal(request.RedirectUri, item.RedirectUri);
    }

    [Fact]
    public async Task Create_WithDuplicateProvider_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOAuthProviderConfigRequestV1
        {
            Provider = OAuthProvider.Microsoft,
            ClientId = "client_id",
            ClientSecret = "client_secret",
            RedirectUri = "https://example.com/callback"
        };

        _configServiceMock
            .Setup(s => s.CreateAsync(
                _userId,
                request.Provider,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth provider config already exists"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithExistingConfig_ReturnsOkWithUpdatedDetail()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdateOAuthProviderConfigRequestV1
        {
            ClientId = "updated_client_id",
            ClientSecret = "updated_client_secret",
            RedirectUri = "https://example.com/new-callback"
        };

        var updatedConfig = new OAuthProviderConfig
        {
            Id = configId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            ClientId = request.ClientId!,
            ClientSecret = request.ClientSecret!,
            RedirectUri = request.RedirectUri!,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configServiceMock
            .Setup(s => s.UpdateAsync(
                _userId,
                configId,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedConfig);

        // Act
        var result = await _controller.Update(configId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<OAuthProviderConfigDetailV1>(okResult.Value);
        Assert.Equal(configId, detail.Id);
        Assert.Equal(request.RedirectUri, detail.RedirectUri);
    }

    [Fact]
    public async Task Update_WithNonExistingConfig_ReturnsNotFound()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var request = new UpdateOAuthProviderConfigRequestV1
        {
            ClientId = "updated_client_id"
        };

        _configServiceMock
            .Setup(s => s.UpdateAsync(
                _userId,
                configId,
                request.ClientId,
                request.ClientSecret,
                request.RedirectUri,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Config not found"));

        // Act
        var result = await _controller.Update(configId, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithExistingConfig_ReturnsNoContent()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _configServiceMock
            .Setup(s => s.DeleteAsync(_userId, configId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(configId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_WithNonExistingConfig_ReturnsNotFound()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _configServiceMock
            .Setup(s => s.DeleteAsync(_userId, configId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Config not found"));

        // Act
        var result = await _controller.Delete(configId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_MasksClientIdAndSecret()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new OAuthProviderConfig
        {
            Id = configId,
            UserId = _userId,
            Provider = OAuthProvider.Google,
            ClientId = "abcd1234efgh5678",
            ClientSecret = "secret_abcd1234efgh5678",
            RedirectUri = "https://example.com/callback",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _configServiceMock
            .Setup(s => s.GetByIdAsync(_userId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.Get(configId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<OAuthProviderConfigDetailV1>(okResult.Value);
        Assert.NotEqual(config.ClientId, detail.ClientId);
        Assert.NotEqual(config.ClientSecret, detail.ClientSecret);
        Assert.Contains("***", detail.ClientId);
        Assert.Contains("***", detail.ClientSecret);
    }
}
