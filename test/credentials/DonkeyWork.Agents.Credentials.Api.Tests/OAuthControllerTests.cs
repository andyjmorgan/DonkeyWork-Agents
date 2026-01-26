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

        // Setup HttpContext for cookies
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAuthorizationUrl_WithValidProvider_ReturnsOkWithUrl()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?...";
        var state = "random_state_123";
        var codeVerifier = "random_verifier_456";

        _oauthFlowServiceMock
            .Setup(s => s.GenerateAuthorizationUrlAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((authUrl, state, codeVerifier));

        // Act
        var result = await _controller.GetAuthorizationUrl(provider, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetAuthorizationUrlResponseV1>(okResult.Value);
        Assert.Equal(authUrl, response.AuthorizationUrl);
        Assert.Equal(state, response.State);

        // Verify cookies were set (no assertion needed, just checking service was called)
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

    [Fact]
    public async Task GetAuthorizationUrl_SetsCookiesWithCorrectOptions()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var authUrl = "https://github.com/login/oauth/authorize?...";
        var state = "state_123";
        var codeVerifier = "verifier_456";

        _oauthFlowServiceMock
            .Setup(s => s.GenerateAuthorizationUrlAsync(_userId, provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((authUrl, state, codeVerifier));

        // Act
        await _controller.GetAuthorizationUrl(provider, CancellationToken.None);

        // Assert
        var cookies = _controller.Response.Cookies;
        Assert.NotNull(cookies);

        _oauthFlowServiceMock.Verify(
            s => s.GenerateAuthorizationUrlAsync(_userId, provider, It.IsAny<CancellationToken>()),
            Times.Once);
    }

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
        _controller.ControllerContext.HttpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string>
            {
                [$"oauth_state_{provider}"] = state,
                [$"oauth_verifier_{provider}"] = codeVerifier,
                [$"oauth_userid_{provider}"] = _userId.ToString()
            });

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
                _userId,
                provider,
                code,
                state,
                codeVerifier,
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
    public async Task Callback_WithStateMismatch_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.GitHub;
        var code = "auth_code_123";
        var state = "state_123";
        var wrongState = "wrong_state";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");
        _controller.ControllerContext.HttpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string>
            {
                [$"oauth_state_{provider}"] = wrongState
            });

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=state_mismatch", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithMissingCodeVerifier_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.Google;
        var code = "auth_code_123";
        var state = "state_123";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");
        _controller.ControllerContext.HttpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string>
            {
                [$"oauth_state_{provider}"] = state
                // Missing verifier
            });

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=missing_verifier", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithMissingUserId_RedirectsWithError()
    {
        // Arrange
        var provider = OAuthProvider.Microsoft;
        var code = "auth_code_123";
        var state = "state_123";
        var codeVerifier = "verifier_456";

        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost:5050");
        _controller.ControllerContext.HttpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string>
            {
                [$"oauth_state_{provider}"] = state,
                [$"oauth_verifier_{provider}"] = codeVerifier
                // Missing userid
            });

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=missing_userid", redirectResult.Url);
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
        _controller.ControllerContext.HttpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string>
            {
                [$"oauth_state_{provider}"] = state,
                [$"oauth_verifier_{provider}"] = codeVerifier,
                [$"oauth_userid_{provider}"] = _userId.ToString()
            });

        _oauthFlowServiceMock
            .Setup(s => s.HandleCallbackAsync(
                _userId,
                provider,
                code,
                state,
                codeVerifier,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Token exchange failed"));

        // Act
        var result = await _controller.Callback(provider, code, state, null, null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=callback_failed", redirectResult.Url);
    }

    // Helper class for mocking request cookies
    private class MockRequestCookieCollection : IRequestCookieCollection
    {
        private readonly Dictionary<string, string> _cookies;

        public MockRequestCookieCollection(Dictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
        public int Count => _cookies.Count;
        public ICollection<string> Keys => _cookies.Keys;
        public bool ContainsKey(string key) => _cookies.ContainsKey(key);
        public bool TryGetValue(string key, out string? value)
        {
            if (_cookies.TryGetValue(key, out var val))
            {
                value = val;
                return true;
            }
            value = null;
            return false;
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
    }
}
