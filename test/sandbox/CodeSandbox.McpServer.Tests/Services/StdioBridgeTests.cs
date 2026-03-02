using CodeSandbox.Executor.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CodeSandbox.McpServer.Tests.Services;

public class StdioBridgeTests
{
    #region IsNotification Tests

    [Fact]
    public void IsNotification_WithIdField_ReturnsFalse()
    {
        var json = """{"jsonrpc":"2.0","id":"1","method":"tools/list"}""";
        Assert.False(StdioBridge.IsNotification(json));
    }

    [Fact]
    public void IsNotification_WithoutIdField_ReturnsTrue()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";
        Assert.True(StdioBridge.IsNotification(json));
    }

    [Fact]
    public void IsNotification_WithNullId_ReturnsFalse()
    {
        // JSON with null id still has the "id" property
        var json = """{"jsonrpc":"2.0","id":null,"method":"test"}""";
        Assert.False(StdioBridge.IsNotification(json));
    }

    [Fact]
    public void IsNotification_InvalidJson_ReturnsFalse()
    {
        Assert.False(StdioBridge.IsNotification("not json"));
    }

    [Fact]
    public void IsNotification_EmptyObject_ReturnsTrue()
    {
        Assert.True(StdioBridge.IsNotification("{}"));
    }

    #endregion

    #region ExtractId Tests

    [Fact]
    public void ExtractId_StringId_ReturnsStringValue()
    {
        var json = """{"jsonrpc":"2.0","id":"request-1","method":"test"}""";
        Assert.Equal("request-1", StdioBridge.ExtractId(json));
    }

    [Fact]
    public void ExtractId_NumericId_ReturnsRawText()
    {
        var json = """{"jsonrpc":"2.0","id":42,"method":"test"}""";
        Assert.Equal("42", StdioBridge.ExtractId(json));
    }

    [Fact]
    public void ExtractId_NoId_ReturnsNull()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";
        Assert.Null(StdioBridge.ExtractId(json));
    }

    [Fact]
    public void ExtractId_InvalidJson_ReturnsNull()
    {
        Assert.Null(StdioBridge.ExtractId("not json"));
    }

    [Fact]
    public void ExtractId_NegativeNumericId_ReturnsRawText()
    {
        var json = """{"jsonrpc":"2.0","id":-1,"method":"test"}""";
        Assert.Equal("-1", StdioBridge.ExtractId(json));
    }

    #endregion

    #region ExtractMethod Tests

    [Fact]
    public void ExtractMethod_WithMethod_ReturnsMethodName()
    {
        var json = """{"jsonrpc":"2.0","id":"1","method":"tools/list"}""";
        Assert.Equal("tools/list", StdioBridge.ExtractMethod(json));
    }

    [Fact]
    public void ExtractMethod_NoMethod_ReturnsNull()
    {
        var json = """{"jsonrpc":"2.0","id":"1","result":{}}""";
        Assert.Null(StdioBridge.ExtractMethod(json));
    }

    [Fact]
    public void ExtractMethod_InvalidJson_ReturnsNull()
    {
        Assert.Null(StdioBridge.ExtractMethod("not json"));
    }

    [Fact]
    public void ExtractMethod_NotificationMethod_ReturnsMethodName()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/progress","params":{}}""";
        Assert.Equal("notifications/progress", StdioBridge.ExtractMethod(json));
    }

    #endregion

    #region IsJsonRpcResponse Tests

    [Fact]
    public void IsJsonRpcResponse_WithResult_ReturnsTrue()
    {
        var json = """{"jsonrpc":"2.0","id":"1","result":{"tools":[]}}""";
        Assert.True(StdioBridge.IsJsonRpcResponse(json));
    }

    [Fact]
    public void IsJsonRpcResponse_WithError_ReturnsTrue()
    {
        var json = """{"jsonrpc":"2.0","id":"1","error":{"code":-32600,"message":"Invalid"}}""";
        Assert.True(StdioBridge.IsJsonRpcResponse(json));
    }

    [Fact]
    public void IsJsonRpcResponse_Request_ReturnsFalse()
    {
        var json = """{"jsonrpc":"2.0","id":"1","method":"tools/list"}""";
        Assert.False(StdioBridge.IsJsonRpcResponse(json));
    }

    [Fact]
    public void IsJsonRpcResponse_Notification_ReturnsFalse()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";
        Assert.False(StdioBridge.IsJsonRpcResponse(json));
    }

    [Fact]
    public void IsJsonRpcResponse_NoJsonRpc_ReturnsFalse()
    {
        var json = """{"result":"success"}""";
        Assert.False(StdioBridge.IsJsonRpcResponse(json));
    }

    [Fact]
    public void IsJsonRpcResponse_InvalidJson_ReturnsFalse()
    {
        Assert.False(StdioBridge.IsJsonRpcResponse("not json"));
    }

    #endregion

    #region TryParseJsonRpc Tests

    [Fact]
    public void TryParseJsonRpc_Request_ParsesCorrectly()
    {
        var json = """{"jsonrpc":"2.0","id":"req-1","method":"tools/list"}""";

        var result = StdioBridge.TryParseJsonRpc(json, out var hasId, out var id, out var isResponse, out var hasMethod);

        Assert.True(result);
        Assert.True(hasId);
        Assert.Equal("req-1", id);
        Assert.False(isResponse);
        Assert.True(hasMethod);
    }

    [Fact]
    public void TryParseJsonRpc_Response_ParsesCorrectly()
    {
        var json = """{"jsonrpc":"2.0","id":"req-1","result":{"tools":[]}}""";

        var result = StdioBridge.TryParseJsonRpc(json, out var hasId, out var id, out var isResponse, out var hasMethod);

        Assert.True(result);
        Assert.True(hasId);
        Assert.Equal("req-1", id);
        Assert.True(isResponse);
        Assert.False(hasMethod);
    }

    [Fact]
    public void TryParseJsonRpc_Notification_ParsesCorrectly()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/progress","params":{}}""";

        var result = StdioBridge.TryParseJsonRpc(json, out var hasId, out var id, out var isResponse, out var hasMethod);

        Assert.True(result);
        Assert.False(hasId);
        Assert.Null(id);
        Assert.False(isResponse);
        Assert.True(hasMethod);
    }

    [Fact]
    public void TryParseJsonRpc_ErrorResponse_ParsesCorrectly()
    {
        var json = """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}""";

        var result = StdioBridge.TryParseJsonRpc(json, out var hasId, out var id, out var isResponse, out var hasMethod);

        Assert.True(result);
        Assert.True(hasId);
        Assert.Equal("1", id);
        Assert.True(isResponse);
        Assert.False(hasMethod);
    }

    [Fact]
    public void TryParseJsonRpc_NoJsonRpcField_ReturnsFalse()
    {
        var json = """{"id":"1","method":"test"}""";

        var result = StdioBridge.TryParseJsonRpc(json, out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseJsonRpc_InvalidJson_ReturnsFalse()
    {
        var result = StdioBridge.TryParseJsonRpc("not json", out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseJsonRpc_EmptyString_ReturnsFalse()
    {
        var result = StdioBridge.TryParseJsonRpc("", out _, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParseJsonRpc_NumericId_ExtractedCorrectly()
    {
        var json = """{"jsonrpc":"2.0","id":999,"result":{}}""";

        StdioBridge.TryParseJsonRpc(json, out var hasId, out var id, out _, out _);

        Assert.True(hasId);
        Assert.Equal("999", id);
    }

    #endregion

    #region State Machine Tests

    [Fact]
    public void State_InitialState_IsIdle()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        Assert.Equal(Executor.Models.McpServerState.Idle, bridge.State);
    }

    [Fact]
    public void GetStatus_InitialState_ReturnsIdleWithNoError()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        var status = bridge.GetStatus();

        Assert.Equal(Executor.Models.McpServerState.Idle, status.State);
        Assert.Null(status.Error);
        Assert.Null(status.StartedAt);
        Assert.Null(status.LastRequestAt);
    }

    [Fact]
    public void GetNotificationReader_BeforeStart_ReturnsNull()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        Assert.Null(bridge.GetNotificationReader());
    }

    [Fact]
    public async Task SendRequestAsync_WhenNotReady_ThrowsInvalidOperationException()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bridge.SendRequestAsync("""{"jsonrpc":"2.0","id":"1","method":"test"}""", 5, CancellationToken.None));
    }

    [Fact]
    public async Task SendResponseAsync_WhenNotReady_ThrowsInvalidOperationException()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bridge.SendResponseAsync("""{"jsonrpc":"2.0","id":"1","result":{}}""", CancellationToken.None));
    }

    [Fact]
    public void Stop_WhenIdle_RemainsIdle()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        bridge.Stop();

        Assert.Equal(Executor.Models.McpServerState.Idle, bridge.State);
    }

    [Fact]
    public async Task StartAsync_WithInvalidCommand_TransitionsToError()
    {
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        using var bridge = new StdioBridge(loggerFactory.CreateLogger<StdioBridge>());

        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            bridge.StartAsync(
                "/nonexistent/command",
                [],
                [],
                5,
                null,
                null,
                CancellationToken.None));

        Assert.Equal(Executor.Models.McpServerState.Error, bridge.State);
        Assert.NotNull(bridge.GetStatus().Error);
    }

    #endregion
}
