using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Mcp.Core.Middleware;
using DonkeyWork.Agents.Mcp.Core.Services;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Mcp.Core.Tests.Middleware;

public class McpTraceMiddlewareTests
{
    private readonly Mock<IMcpTraceRepository> _repositoryMock;
    private readonly Mock<ILogger<McpTraceMiddleware>> _loggerMock;

    public McpTraceMiddlewareTests()
    {
        _repositoryMock = new Mock<IMcpTraceRepository>();
        _loggerMock = new Mock<ILogger<McpTraceMiddleware>>();
    }

    private McpTraceMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new McpTraceMiddleware(next, _loggerMock.Object);
    }

    private DefaultHttpContext CreateMcpContext(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/";
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        var services = new ServiceCollection();
        services.AddSingleton(_repositoryMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    #region Request Filtering Tests

    [Fact]
    public async Task InvokeAsync_GetRequest_SkipsTracing()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.ContentType = "application/json";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<McpTraceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_NonJsonContentType_SkipsTracing()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "text/html";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<McpTraceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_NonJsonRpcBody_SkipsTracing()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateMcpContext("""{"hello":"world"}""");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<McpTraceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_McpPath_TracesRequest()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/list", id = "1" });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return ctx.Response.WriteAsync("""{"jsonrpc":"2.0","result":{},"id":"1"}""");
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e =>
                e.Method == "tools/list" &&
                e.JsonRpcId == "1" &&
                e.HttpStatusCode == 200 &&
                e.IsSuccess),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region JSON-RPC Extraction Tests

    [Fact]
    public async Task InvokeAsync_ToolsCallRequest_ExtractsMethodAndId()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/call", id = "42", @params = new { name = "my_tool" } });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return ctx.Response.WriteAsync("""{"jsonrpc":"2.0","result":{},"id":"42"}""");
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => e.Method == "tools/call" && e.JsonRpcId == "42"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NumericJsonRpcId_ExtractedAsString()
    {
        // Arrange
        var requestBody = """{"jsonrpc":"2.0","method":"initialize","id":7}""";
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return ctx.Response.WriteAsync("""{"jsonrpc":"2.0","result":{},"id":7}""");
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => e.Method == "initialize" && e.JsonRpcId == "7"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_InvalidJson_SkipsTracing()
    {
        // Arrange
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateMcpContext("not valid json at all");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<McpTraceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_JsonRpcWithoutMethod_StillTraces()
    {
        // Arrange
        var requestBody = """{"jsonrpc":"2.0","result":{},"id":"1"}""";
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => e.Method == "unknown"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Response Capture Tests

    [Fact]
    public async Task InvokeAsync_ErrorResponse_SetsIsSuccessFalse()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/call", id = "1" });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => !e.IsSuccess && e.HttpStatusCode == 401),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_JsonRpcError_ExtractsErrorMessage()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/call", id = "1" });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return ctx.Response.WriteAsync("""{"jsonrpc":"2.0","error":{"code":-32600,"message":"Invalid request"},"id":"1"}""");
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => e.IsSuccess && e.ResponseBody!.Contains("Invalid request")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_CapturesRequestAndResponseBodies()
    {
        // Arrange
        var requestBody = """{"jsonrpc":"2.0","method":"tools/list","id":"abc"}""";
        var responseBody = """{"jsonrpc":"2.0","result":{"tools":[]},"id":"abc"}""";
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return ctx.Response.WriteAsync(responseBody);
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e =>
                e.RequestBody == requestBody &&
                e.ResponseBody == responseBody),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Timing Tests

    [Fact]
    public async Task InvokeAsync_RecordsDurationAndTimestamps()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "initialize", id = "1" });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e =>
                e.StartedAt != default &&
                e.CompletedAt != null &&
                e.DurationMs != null &&
                e.DurationMs >= 0),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Resilience Tests

    [Fact]
    public async Task InvokeAsync_RepositoryThrows_DoesNotBreakPipeline()
    {
        // Arrange
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<McpTraceEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB failure"));

        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/list", id = "1" });
        var context = CreateMcpContext(requestBody);

        var responseWritten = false;
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            responseWritten = true;
            return Task.CompletedTask;
        });

        // Act — should not throw
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(responseWritten);
    }

    [Fact]
    public async Task InvokeAsync_InnerPipelineThrows_StillTracesAndRethrows()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "tools/call", id = "1" });
        var context = CreateMcpContext(requestBody);

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    #endregion

    #region ExtractJsonRpcFields Tests

    [Fact]
    public void ExtractJsonRpcFields_ValidJson_ReturnsMethodAndId()
    {
        var (method, id) = McpTraceMiddleware.ExtractJsonRpcFields("""{"method":"tools/call","id":"abc"}""");
        Assert.Equal("tools/call", method);
        Assert.Equal("abc", id);
    }

    [Fact]
    public void ExtractJsonRpcFields_EmptyString_ReturnsNulls()
    {
        var (method, id) = McpTraceMiddleware.ExtractJsonRpcFields("");
        Assert.Null(method);
        Assert.Null(id);
    }

    [Fact]
    public void ExtractJsonRpcFields_NoMethodField_ReturnsNullMethod()
    {
        var (method, id) = McpTraceMiddleware.ExtractJsonRpcFields("""{"id":"1"}""");
        Assert.Null(method);
        Assert.Equal("1", id);
    }

    [Fact]
    public void ExtractJsonRpcFields_NumericId_ReturnsRawText()
    {
        var (method, id) = McpTraceMiddleware.ExtractJsonRpcFields("""{"method":"initialize","id":42}""");
        Assert.Equal("initialize", method);
        Assert.Equal("42", id);
    }

    #endregion

    #region Client Metadata Tests

    [Fact]
    public async Task InvokeAsync_CapturesUserAgent()
    {
        // Arrange
        var requestBody = JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "initialize", id = "1" });
        var context = CreateMcpContext(requestBody);
        context.Request.Headers.UserAgent = "Claude/1.0";

        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(
            It.Is<McpTraceEntity>(e => e.UserAgent == "Claude/1.0"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
