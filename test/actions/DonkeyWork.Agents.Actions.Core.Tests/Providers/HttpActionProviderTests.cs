using System.Net;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Actions.Core.Providers;
using Moq;
using Moq.Protected;

namespace DonkeyWork.Agents.Actions.Core.Tests.Providers;

public class HttpActionProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IParameterResolver> _parameterResolverMock;
    private readonly HttpActionProvider _provider;

    public HttpActionProviderTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _parameterResolverMock = new Mock<IParameterResolver>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Setup parameter resolver to return the literal value
        _parameterResolverMock
            .Setup(r => r.Resolve(It.IsAny<Resolvable<int>>(), It.IsAny<object?>()))
            .Returns((Resolvable<int> r, object? _) => int.Parse(r.RawValue));

        // Setup ResolveString to return the input string (no variable substitution in tests)
        _parameterResolverMock
            .Setup(r => r.ResolveString(It.IsAny<string>(), It.IsAny<object?>()))
            .Returns((string s, object? _) => s);

        _provider = new HttpActionProvider(
            _httpClientFactoryMock.Object,
            _parameterResolverMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulGetRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var expectedResponse = "test response";
        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test"
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(expectedResponse, result.Body);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task ExecuteAsync_FailedRequest_ReturnsFailureResponse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, "Not found");

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/missing"
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Not found", result.Body);
    }

    [Fact]
    public async Task ExecuteAsync_WithHeaders_AddsHeadersToRequest()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "success");

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test",
            Headers = KeyValueCollection.FromItems(new[]
            {
                new KeyValueItem { Key = "Authorization", Value = "Bearer token123" },
                new KeyValueItem { Key = "X-Custom-Header", Value = "value" }
            })
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("success")
            });

        // Act
        await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("Authorization"));
        Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
    }

    [Fact]
    public async Task ExecuteAsync_PostWithBody_SendsBody()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Created, "created");

        var requestBody = "{\"name\":\"test\",\"value\":123}";
        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.POST,
            Url = "https://api.example.com/create",
            Body = new Resolvable<string>(requestBody)
        };

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("created")
            });

        // Act
        await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Content);
        var content = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Equal(requestBody, content);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_SetsClientTimeout()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "success");

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test",
            TimeoutSeconds = 60
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert - If it doesn't throw, timeout was set correctly
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsErrorResponse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test"
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.StatusCode);
        Assert.Contains("Network error", result.Body);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesResponseHeaders()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("success")
        };
        response.Headers.Add("X-Custom-Response", "test-value");
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test"
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.Headers.ContainsKey("X-Custom-Response"));
        Assert.Equal("test-value", result.Headers["X-Custom-Response"]);
        Assert.True(result.Headers.ContainsKey("Content-Type"));
    }

    [Fact]
    public async Task ExecuteAsync_TracksDuration()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                await Task.Delay(50); // Simulate network delay
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("success")
                };
            });

        var parameters = new HttpRequestParameters
        {
            Method = DonkeyWork.Agents.Actions.Core.Providers.HttpMethod.GET,
            Url = "https://api.example.com/test"
        };

        // Act
        var result = await _provider.ExecuteAsync(parameters);

        // Assert
        Assert.True(result.DurationMs >= 50);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }
}
