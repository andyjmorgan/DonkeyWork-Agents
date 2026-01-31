using System.Net;
using System.Text.Json;
using DonkeyWork.Agents.Identity.Api.Controllers;
using DonkeyWork.Agents.Identity.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace DonkeyWork.Agents.Identity.Api.Tests;

public class AuthControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IOptions<KeycloakOptions> _keycloakOptions;
    private readonly KeycloakOptions _keycloakOptionsValue;

    public AuthControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _keycloakOptionsValue = new KeycloakOptions
        {
            Authority = "https://auth.example.com/realms/test",
            Audience = "test-client"
        };
        _keycloakOptions = Microsoft.Extensions.Options.Options.Create(_keycloakOptionsValue);
    }

    private AuthController CreateController(HttpContext? httpContext = null)
    {
        var controller = new AuthController(_keycloakOptions, _httpClientFactoryMock.Object);

        if (httpContext != null)
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        return controller;
    }

    private static DefaultHttpContext CreateHttpContext(string scheme = "https", string host = "localhost")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return context;
    }

    [Fact]
    public void Login_RedirectsToKeycloakWithPkceParameters()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var controller = CreateController(httpContext);

        // Act
        var result = controller.Login();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains(_keycloakOptionsValue.Authority, redirectResult.Url);
        Assert.Contains("client_id=" + Uri.EscapeDataString(_keycloakOptionsValue.Audience), redirectResult.Url);
        Assert.Contains("response_type=code", redirectResult.Url);
        Assert.Contains("code_challenge=", redirectResult.Url);
        Assert.Contains("code_challenge_method=S256", redirectResult.Url);
    }

    [Fact]
    public void Login_SetsCodeVerifierCookie()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var controller = CreateController(httpContext);

        // Act
        controller.Login();

        // Assert
        Assert.True(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("pkce_code_verifier=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_IncludesCorrectRedirectUri()
    {
        // Arrange
        var httpContext = CreateHttpContext("https", "api.example.com");
        var controller = CreateController(httpContext);

        // Act
        var result = controller.Login();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        var expectedRedirectUri = Uri.EscapeDataString("https://api.example.com/api/v1/auth/callback");
        Assert.Contains($"redirect_uri={expectedRedirectUri}", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithError_RedirectsWithError()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback(null, "access_denied", "User denied access");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=access_denied", redirectResult.Url);
        Assert.Contains("error_description=", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithoutCode_RedirectsWithError()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback(null, null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=missing_code", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithoutCodeVerifierCookie_RedirectsWithError()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback("valid-code", null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=missing_verifier", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithValidCodeAndVerifier_RedirectsWithTokens()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string> { { "pkce_code_verifier", "test-verifier" } });

        var tokenResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        var mockHandler = CreateMockHttpHandler(new[]
        {
            (HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse))
        });

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback("valid-code", null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("access_token=test-access-token", redirectResult.Url);
        Assert.Contains("refresh_token=test-refresh-token", redirectResult.Url);
        Assert.Contains("expires_in=3600", redirectResult.Url);
        Assert.Contains("token_type=Bearer", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WhenTokenExchangeFails_RedirectsWithError()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string> { { "pkce_code_verifier", "test-verifier" } });

        var mockHandler = CreateMockHttpHandler(new[]
        {
            (HttpStatusCode.BadRequest, "{\"error\": \"invalid_grant\"}")
        });

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback("invalid-code", null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("error=token_exchange_failed", redirectResult.Url);
    }

    [Fact]
    public async Task Callback_WithoutRefreshToken_RedirectsWithAccessTokenOnly()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Cookies = new MockRequestCookieCollection(
            new Dictionary<string, string> { { "pkce_code_verifier", "test-verifier" } });

        var tokenResponse = new
        {
            access_token = "test-access-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        var mockHandler = CreateMockHttpHandler(new[]
        {
            (HttpStatusCode.OK, JsonSerializer.Serialize(tokenResponse))
        });

        var httpClient = new HttpClient(mockHandler.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var controller = CreateController(httpContext);

        // Act
        var result = await controller.Callback("valid-code", null, null);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("access_token=test-access-token", redirectResult.Url);
        Assert.Contains("expires_in=3600", redirectResult.Url);
        Assert.DoesNotContain("refresh_token=", redirectResult.Url);
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(
        (HttpStatusCode StatusCode, string Content)[] responses)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var callIndex = 0;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var (statusCode, content) = responses[callIndex];
                callIndex = Math.Min(callIndex + 1, responses.Length - 1);

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content)
                };
            });

        return handlerMock;
    }

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

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out string value)
        {
            if (_cookies.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }
            value = null!;
            return false;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
    }
}
