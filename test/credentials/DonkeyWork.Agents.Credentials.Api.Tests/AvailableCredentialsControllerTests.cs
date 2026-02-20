using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests;

public class AvailableCredentialsControllerTests
{
    private readonly Mock<IOAuthTokenService> _tokenServiceMock;
    private readonly Mock<IExternalApiKeyService> _externalApiKeyServiceMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly AvailableCredentialsController _controller;
    private readonly Guid _userId;

    public AvailableCredentialsControllerTests()
    {
        _tokenServiceMock = new Mock<IOAuthTokenService>();
        _externalApiKeyServiceMock = new Mock<IExternalApiKeyService>();
        _identityContextMock = new Mock<IIdentityContext>();
        _userId = Guid.NewGuid();

        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _controller = new AvailableCredentialsController(
            _tokenServiceMock.Object,
            _externalApiKeyServiceMock.Object,
            _identityContextMock.Object);
    }

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllProviderTypes()
    {
        // Arrange
        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthToken>());

        _externalApiKeyServiceMock
            .Setup(s => s.GetConfiguredLlmProvidersAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalApiKeyProvider>());

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<AvailableCredentialV1>>(okResult.Value);

        var oauthProviderCount = Enum.GetValues<OAuthProvider>().Length;
        var apiKeyProviderCount = Enum.GetValues<ExternalApiKeyProvider>().Length;
        Assert.Equal(oauthProviderCount + apiKeyProviderCount, items.Count);

        // Verify all OAuth providers are present
        foreach (var provider in Enum.GetValues<OAuthProvider>())
        {
            Assert.Contains(items, i => i.CredentialType == "OAuth" && i.Provider == provider.ToString());
        }

        // Verify all API key providers are present
        foreach (var provider in Enum.GetValues<ExternalApiKeyProvider>())
        {
            Assert.Contains(items, i => i.CredentialType == "ApiKey" && i.Provider == provider.ToString());
        }
    }

    [Fact]
    public async Task List_WithConfiguredOAuth_SetsIsConfiguredTrue()
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
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                Scopes = new[] { "email" },
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        _externalApiKeyServiceMock
            .Setup(s => s.GetConfiguredLlmProvidersAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalApiKeyProvider> { ExternalApiKeyProvider.OpenAI });

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<AvailableCredentialV1>>(okResult.Value);

        // Google OAuth should be configured
        var googleOAuth = items.Single(i => i.CredentialType == "OAuth" && i.Provider == "Google");
        Assert.True(googleOAuth.IsConfigured);

        // Microsoft OAuth should not be configured
        var microsoftOAuth = items.Single(i => i.CredentialType == "OAuth" && i.Provider == "Microsoft");
        Assert.False(microsoftOAuth.IsConfigured);

        // OpenAI API key should be configured
        var openAiApiKey = items.Single(i => i.CredentialType == "ApiKey" && i.Provider == "OpenAI");
        Assert.True(openAiApiKey.IsConfigured);

        // Anthropic API key should not be configured
        var anthropicApiKey = items.Single(i => i.CredentialType == "ApiKey" && i.Provider == "Anthropic");
        Assert.False(anthropicApiKey.IsConfigured);
    }

    [Fact]
    public async Task List_WithNoCredentials_AllSetToFalse()
    {
        // Arrange
        _tokenServiceMock
            .Setup(s => s.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthToken>());

        _externalApiKeyServiceMock
            .Setup(s => s.GetConfiguredLlmProvidersAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalApiKeyProvider>());

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<AvailableCredentialV1>>(okResult.Value);

        Assert.All(items, item => Assert.False(item.IsConfigured));
    }

    #endregion
}
