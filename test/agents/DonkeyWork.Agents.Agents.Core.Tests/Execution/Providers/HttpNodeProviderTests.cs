using System.Net;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Providers;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Types;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Providers;

/// <summary>
/// Unit tests for HttpNodeProvider.
/// Tests HTTP request execution with mocked HttpClient.
/// </summary>
public class HttpNodeProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IExecutionContext> _executionContextMock;
    private readonly Mock<ILogger<HttpNodeProvider>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public HttpNodeProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _executionContextMock = new Mock<IExecutionContext>();
        _loggerMock = new Mock<ILogger<HttpNodeProvider>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _executionContextMock.Setup(c => c.ExecutionId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(new Dictionary<string, object>());
    }

    private HttpNodeProvider CreateProvider(HttpResponseMessage response)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new HttpNodeProvider(_httpClientFactoryMock.Object, _executionContextMock.Object, _loggerMock.Object);
    }

    #region ExecuteHttpRequestAsync Tests

    [Fact]
    public async Task ExecuteHttpRequestAsync_WithGetRequest_ReturnsResponse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}")
        };
        var provider = CreateProvider(response);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = Contracts.Nodes.Enums.HttpMethod.Get,
            Url = "https://api.example.com/data",
            TimeoutSeconds = 30
        };

        // Act
        var result = await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("success", result.Body);
    }

    [Fact]
    public async Task ExecuteHttpRequestAsync_WithPostRequest_SendsBody()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var provider = new HttpNodeProvider(_httpClientFactoryMock.Object, _executionContextMock.Object, _loggerMock.Object);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = Contracts.Nodes.Enums.HttpMethod.Post,
            Url = "https://api.example.com/data",
            Body = "{\"test\":\"data\"}",
            TimeoutSeconds = 30
        };

        // Act
        await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(System.Net.Http.HttpMethod.Post, capturedRequest!.Method);
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("test", body);
    }

    [Fact]
    public async Task ExecuteHttpRequestAsync_WithHeaders_IncludesHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var provider = new HttpNodeProvider(_httpClientFactoryMock.Object, _executionContextMock.Object, _loggerMock.Object);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = Contracts.Nodes.Enums.HttpMethod.Get,
            Url = "https://api.example.com/data",
            Headers = new KeyValueCollection
            {
                Items = new List<KeyValueItem>
                {
                    new() { Key = "Authorization", Value = "Bearer token123" },
                    new() { Key = "X-Custom-Header", Value = "custom-value" }
                }
            },
            TimeoutSeconds = 30
        };

        // Act
        await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("Authorization"));
        Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
    }

    [Fact]
    public async Task ExecuteHttpRequestAsync_WithTemplateInUrl_ResolvesTemplate()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { id = "123" });

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var provider = new HttpNodeProvider(_httpClientFactoryMock.Object, _executionContextMock.Object, _loggerMock.Object);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = Contracts.Nodes.Enums.HttpMethod.Get,
            Url = "https://api.example.com/items/{{ input.id }}",
            TimeoutSeconds = 30
        };

        // Act
        await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.example.com/items/123", capturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExecuteHttpRequestAsync_ReturnsResponseHeaders()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };
        response.Headers.Add("X-Response-Id", "resp-123");
        var provider = CreateProvider(response);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = Contracts.Nodes.Enums.HttpMethod.Get,
            Url = "https://api.example.com/data",
            TimeoutSeconds = 30
        };

        // Act
        var result = await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Headers);
        Assert.True(result.Headers.ContainsKey("X-Response-Id"));
    }

    [Theory]
    [InlineData(Contracts.Nodes.Enums.HttpMethod.Get)]
    [InlineData(Contracts.Nodes.Enums.HttpMethod.Post)]
    [InlineData(Contracts.Nodes.Enums.HttpMethod.Put)]
    [InlineData(Contracts.Nodes.Enums.HttpMethod.Delete)]
    [InlineData(Contracts.Nodes.Enums.HttpMethod.Patch)]
    public async Task ExecuteHttpRequestAsync_WithAllMethods_UsesCorrectHttpMethod(Contracts.Nodes.Enums.HttpMethod method)
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var provider = new HttpNodeProvider(_httpClientFactoryMock.Object, _executionContextMock.Object, _loggerMock.Object);

        var config = new HttpRequestNodeConfiguration
        {
            Name = "http_1",
            Method = method,
            Url = "https://api.example.com/data",
            TimeoutSeconds = 30
        };

        // Act
        await provider.ExecuteHttpRequestAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        var expectedMethod = method switch
        {
            Contracts.Nodes.Enums.HttpMethod.Get => System.Net.Http.HttpMethod.Get,
            Contracts.Nodes.Enums.HttpMethod.Post => System.Net.Http.HttpMethod.Post,
            Contracts.Nodes.Enums.HttpMethod.Put => System.Net.Http.HttpMethod.Put,
            Contracts.Nodes.Enums.HttpMethod.Delete => System.Net.Http.HttpMethod.Delete,
            Contracts.Nodes.Enums.HttpMethod.Patch => System.Net.Http.HttpMethod.Patch,
            _ => throw new ArgumentException($"Unhandled method: {method}")
        };
        Assert.Equal(expectedMethod, capturedRequest!.Method);
    }

    #endregion
}
