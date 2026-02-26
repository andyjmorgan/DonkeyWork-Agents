using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Persistence.SeaweedFs.Configuration;
using Orleans.Persistence.SeaweedFs.Provider;
using Orleans.Runtime;
using Orleans.Storage;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Foundation;

public class SeaweedFsGrainStorageTests
{
    private readonly SeaweedFsStorageOptions _options;
    private readonly Mock<ILogger<SeaweedFsGrainStorage>> _loggerMock;

    public SeaweedFsGrainStorageTests()
    {
        _options = new SeaweedFsStorageOptions
        {
            BaseUrl = "http://localhost:8888",
            BasePath = "/test/grain-state"
        };
        _loggerMock = new Mock<ILogger<SeaweedFsGrainStorage>>();
    }

    private (SeaweedFsGrainStorage Storage, MockHttpMessageHandler Handler) CreateStorage(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? responseBody = null,
        string? etag = null)
    {
        var handler = new MockHttpMessageHandler(statusCode, responseBody, etag);
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var storage = new SeaweedFsGrainStorage("test", _options, factory.Object, _loggerMock.Object);
        return (storage, handler);
    }

    private static GrainId CreateGrainId(string type = "TestGrain", string key = "test-key") =>
        GrainId.Create(type, key);

    #region ReadStateAsync Tests

    [Fact]
    public async Task ReadStateAsync_NoExistingState_ReturnsDefaultState()
    {
        // Arrange
        var (storage, _) = CreateStorage(HttpStatusCode.NotFound);
        var grainState = new TestGrainState<TestState>();

        // Act
        await storage.ReadStateAsync("state", CreateGrainId(), grainState);

        // Assert
        Assert.NotNull(grainState.State);
        Assert.False(grainState.RecordExists);
        Assert.Null(grainState.ETag);
    }

    [Fact]
    public async Task ReadStateAsync_ExistingState_DeserializesCorrectly()
    {
        // Arrange
        var expectedState = new TestState { Name = "test", Value = 42 };
        var json = JsonSerializer.Serialize(expectedState);
        var (storage, _) = CreateStorage(HttpStatusCode.OK, json, "\"etag-123\"");
        var grainState = new TestGrainState<TestState>();

        // Act
        await storage.ReadStateAsync("state", CreateGrainId(), grainState);

        // Assert
        Assert.Equal("test", grainState.State.Name);
        Assert.Equal(42, grainState.State.Value);
        Assert.True(grainState.RecordExists);
        Assert.Equal("\"etag-123\"", grainState.ETag);
    }

    #endregion

    #region WriteStateAsync Tests

    [Fact]
    public async Task WriteStateAsync_ValidState_SendsPutRequest()
    {
        // Arrange
        var (storage, handler) = CreateStorage(HttpStatusCode.OK, null, "\"new-etag\"");
        var grainState = new TestGrainState<TestState> { State = new TestState { Name = "written", Value = 99 } };

        // Act
        await storage.WriteStateAsync("state", CreateGrainId(), grainState);

        // Assert
        Assert.True(grainState.RecordExists);
        Assert.Equal("\"new-etag\"", grainState.ETag);
        Assert.Equal(HttpMethod.Put, handler.LastRequest?.Method);
        Assert.Contains("/test/grain-state/", handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task WriteStateAsync_ServerError_ThrowsException()
    {
        // Arrange
        var (storage, _) = CreateStorage(HttpStatusCode.InternalServerError);
        var grainState = new TestGrainState<TestState> { State = new TestState { Name = "fail" } };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            storage.WriteStateAsync("state", CreateGrainId(), grainState));
    }

    #endregion

    #region ClearStateAsync Tests

    [Fact]
    public async Task ClearStateAsync_ExistingState_SendsDeleteAndResetsState()
    {
        // Arrange
        var (storage, handler) = CreateStorage(HttpStatusCode.OK);
        var grainState = new TestGrainState<TestState>
        {
            State = new TestState { Name = "to-clear" },
            RecordExists = true,
            ETag = "\"old-etag\""
        };

        // Act
        await storage.ClearStateAsync("state", CreateGrainId(), grainState);

        // Assert
        Assert.False(grainState.RecordExists);
        Assert.Null(grainState.ETag);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task ClearStateAsync_NotFound_DoesNotThrow()
    {
        // Arrange
        var (storage, _) = CreateStorage(HttpStatusCode.NotFound);
        var grainState = new TestGrainState<TestState> { State = new TestState() };

        // Act (should not throw)
        await storage.ClearStateAsync("state", CreateGrainId(), grainState);

        // Assert
        Assert.False(grainState.RecordExists);
    }

    #endregion

    #region Path Building Tests

    [Fact]
    public async Task ReadStateAsync_BuildsCorrectPath()
    {
        // Arrange
        var (storage, handler) = CreateStorage(HttpStatusCode.NotFound);
        var grainState = new TestGrainState<TestState>();
        var grainId = GrainId.Create("MyGrain", "my-key-123");

        // Act
        await storage.ReadStateAsync("myState", grainId, grainState);

        // Assert
        var requestUrl = handler.LastRequest?.RequestUri?.ToString();
        Assert.Contains("/test/grain-state/MyGrain/my-key-123/myState.json", requestUrl);
    }

    #endregion
}

public class TestState
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public class TestGrainState<T> : IGrainState<T> where T : new()
{
    public T State { get; set; } = new();
    public string? ETag { get; set; }
    public bool RecordExists { get; set; }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _responseBody;
    private readonly string? _etag;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(HttpStatusCode statusCode, string? responseBody = null, string? etag = null)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
        _etag = etag;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = new HttpResponseMessage(_statusCode);
        if (_responseBody != null)
            response.Content = new StringContent(_responseBody, Encoding.UTF8, "application/json");
        if (_etag != null)
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(_etag);

        return Task.FromResult(response);
    }
}
