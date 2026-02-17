using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Api.Controllers;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests;

public class OAuthControllerTests
{
    private readonly Mock<IOAuthFlowService> _oauthFlowServiceMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly Mock<ILogger<OAuthController>> _loggerMock;
    private readonly OAuthController _controller;
    private readonly Guid _userId;

    public OAuthControllerTests()
    {
        _oauthFlowServiceMock = new Mock<IOAuthFlowService>();
        _identityContextMock = new Mock<IIdentityContext>();
        _loggerMock = new Mock<ILogger<OAuthController>>();
        _userId = Guid.NewGuid();

        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _controller = new OAuthController(
            _oauthFlowServiceMock.Object,
            _identityContextMock.Object,
            _loggerMock.Object);

        // Setup HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetAuthorizationUrl Tests

    [Fact]
    public async Task GetAuthorizationUrl_WithValidProvider_ReturnsOkWithUrl()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?...";
        var state = "random_state_123";

        _oauthFlowServiceMock
            .Setup(s => s.GenerateAuthorizationUrlAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((authUrl, state));

        // Act
        var result = await _controller.GetAuthorizationUrl(provider, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetAuthorizationUrlResponseV1>(okResult.Value);
        Assert.Equal(authUrl, response.AuthorizationUrl);
        Assert.Equal(state, response.State);
    }

    [Fact]
    public async Task GetAuthorizationUrl_WithMissingProviderConfig_ReturnsBadRequest()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;

        _oauthFlowServiceMock
            .Setup(s => s.GenerateAuthorizationUrlAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth provider configuration not found"));

        // Act
        var result = await _controller.GetAuthorizationUrl(provider, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Callback Tests

    [Fact]
    public async Task Callback_WithSuccessfulFlow_RedirectsToFrontendWithSuccess()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_123";
        var codeVerifier = "verifier_456";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        var callbackState = new OAuthCallbackState
        {
            UserId = _userId,
            Provider = provider,
            CodeVerifier = codeVerifier
        };

        _oauthFlowServiceMock
            .Setup(s => s.ValidateAndConsumeStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callbackState);

        var token = new OAuthToken
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Provider = provider,
            ExternalUserId = "ext_123",
            Email = "user@example.com",
            AccessToken = "access_token",
            RefreshToken = "refresh_token",
            Scopes = new[] { "email", "profile" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _oauthFlowServiceMock
            .Setup(s => s.HandleCallbackAsync(
                _userId, provider, code, codeVerifier,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("success=true", redirectResult.Url);
        Assert.Contains($"provider={provider}", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_SetsIdentityContextFromState()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_123";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        var callbackState = new OAuthCallbackState
        {
            UserId = _userId,
            Provider = provider,
            CodeVerifier = "verifier_456"
        };

        _oauthFlowServiceMock
            .Setup(s => s.ValidateAndConsumeStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callbackState);

        _oauthFlowServiceMock
            .Setup(s => s.HandleCallbackAsync(
                It.IsAny<Guid>(), It.IsAny<OAuthProvider>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthToken
            {
                Id = Guid.NewGuid(), UserId = _userId, Provider = provider,
                ExternalUserId = "ext", Email = "a@b.com", AccessToken = "t",
                RefreshToken = "r", Scopes = Array.Empty<string>(),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), CreatedAt = DateTimeOffset.UtcNow
            });

        // Act
        await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        _identityContextMock.Verify(x => x.SetIdentity(_userId, null, null, null), Times.Once);
    }

    [Fact]
    public async Task Callback_WithErrorFromProvider_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var error = "access_denied";
        var errorDescription = "User denied access";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        // Act
        var result = await _controller.Callback(provider, null, null, error, errorDescription, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains($"error={error}", redirectResult.Url);
        Assert.Contains($"provider={provider}", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithMissingCode_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.Google;

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        // Act
        var result = await _controller.Callback(provider, null, null, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=missing_parameters", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithInvalidState_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var code = "auth_code_123";
        var state = "invalid_state";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        _oauthFlowServiceMock
            .Setup(s => s.ValidateAndConsumeStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCallbackState?)null);

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=invalid_state", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithProviderMismatch_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_123";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        // State says GitHub but URL says Google
        var callbackState = new OAuthCallbackState
        {
            UserId = _userId,
            Provider = OAuthProvider.GitHub,
            CodeVerifier = "verifier_456"
        };

        _oauthFlowServiceMock
            .Setup(s => s.ValidateAndConsumeStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callbackState);

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=provider_mismatch", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithFlowException_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var code = "auth_code_123";
        var state = "state_123";
        var codeVerifier = "verifier_456";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");

        var callbackState = new OAuthCallbackState
        {
            UserId = _userId,
            Provider = provider,
            CodeVerifier = codeVerifier
        };

        _oauthFlowServiceMock
            .Setup(s => s.ValidateAndConsumeStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(callbackState);

        _oauthFlowServiceMock
            .Setup(s => s.HandleCallbackAsync(
                _userId, provider, code, codeVerifier,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Token exchange failed"));

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=callback_failed", redirectResult.Url);
    }

    #endregion
}
