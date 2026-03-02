using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools;

public class McpToolProviderTests : IAsyncDisposable
{
    private readonly McpToolProvider _provider;
    private readonly Mock<ILogger> _loggerMock;

    public McpToolProviderTests()
    {
        _provider = new McpToolProvider();
        _loggerMock = new Mock<ILogger>();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    #region HasTool Tests

    [Fact]
    public void HasTool_NoToolsLoaded_ReturnsFalse()
    {
        Assert.False(_provider.HasTool("nonexistent"));
    }

    [Fact]
    public void HasTool_EmptyString_ReturnsFalse()
    {
        Assert.False(_provider.HasTool(""));
    }

    #endregion

    #region GetDisplayName Tests

    [Fact]
    public void GetDisplayName_NoToolsLoaded_ReturnsNull()
    {
        Assert.Null(_provider.GetDisplayName("nonexistent"));
    }

    #endregion

    #region GetToolDefinitions Tests

    [Fact]
    public void GetToolDefinitions_NoToolsLoaded_ReturnsEmptyList()
    {
        var definitions = _provider.GetToolDefinitions();

        Assert.Empty(definitions);
    }

    [Fact]
    public void GetToolDefinitions_CalledTwice_ReturnsSameInstance()
    {
        var first = _provider.GetToolDefinitions();
        var second = _provider.GetToolDefinitions();

        Assert.Same(first, second);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsError()
    {
        var args = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}");

        var result = await _provider.ExecuteAsync("nonexistent-tool", args, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_NoClients_CompletesWithoutError()
    {
        var provider = new McpToolProvider();

        await provider.DisposeAsync();

        // After disposal, everything should be cleared
        Assert.Empty(provider.GetToolDefinitions());
        Assert.False(provider.HasTool("any"));
    }

    #endregion

    #region InitializeAsync Callback Tests

    [Fact]
    public async Task InitializeAsync_EmptyConfigs_DoesNotInvokeCallback()
    {
        var callbackInvoked = false;

        await _provider.InitializeAsync(
            Array.Empty<McpConnectionConfigV1>(),
            Array.Empty<McpStdioConnectionConfigV1>(),
            null,
            "test-user",
            _loggerMock.Object,
            (_, _, _, _, _) => callbackInvoked = true,
            CancellationToken.None);

        Assert.False(callbackInvoked);
    }

    [Fact]
    public async Task InitializeAsync_CancelledToken_InvokesCallbackWithFailure()
    {
        var callbackResults = new List<(string Name, bool Success, string? Error)>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var configs = new[]
        {
            new McpConnectionConfigV1
            {
                Id = Guid.NewGuid(),
                Name = "cancelled-server",
                Endpoint = "http://localhost:19995/mcp",
            },
        };

        await _provider.InitializeAsync(
            configs,
            Array.Empty<McpStdioConnectionConfigV1>(),
            null,
            "test-user",
            _loggerMock.Object,
            (name, success, _, _, error) => callbackResults.Add((name, success, error)),
            cts.Token);

        Assert.Single(callbackResults);
        Assert.False(callbackResults[0].Success);
    }

    [Fact]
    public async Task InitializeAsync_MultipleCancelledServers_InvokesCallbackForEach()
    {
        var callbackNames = new List<string>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var configs = new[]
        {
            new McpConnectionConfigV1
            {
                Id = Guid.NewGuid(),
                Name = "server-a",
                Endpoint = "http://localhost:19998/mcp",
            },
            new McpConnectionConfigV1
            {
                Id = Guid.NewGuid(),
                Name = "server-b",
                Endpoint = "http://localhost:19997/mcp",
            },
        };

        await _provider.InitializeAsync(
            configs,
            Array.Empty<McpStdioConnectionConfigV1>(),
            null,
            "test-user",
            _loggerMock.Object,
            (name, _, _, _, _) => callbackNames.Add(name),
            cts.Token);

        Assert.Equal(2, callbackNames.Count);
        Assert.Contains("server-a", callbackNames);
        Assert.Contains("server-b", callbackNames);
    }

    [Fact]
    public async Task InitializeAsync_CancelledToken_DoesNotAddToolDefinitions()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var configs = new[]
        {
            new McpConnectionConfigV1
            {
                Id = Guid.NewGuid(),
                Name = "dead-server",
                Endpoint = "http://localhost:19994/mcp",
            },
        };

        await _provider.InitializeAsync(
            configs,
            Array.Empty<McpStdioConnectionConfigV1>(),
            null,
            "test-user",
            _loggerMock.Object,
            null,
            cts.Token);

        Assert.Empty(_provider.GetToolDefinitions());
        Assert.False(_provider.HasTool("any-tool"));
    }

    [Fact]
    public async Task InitializeAsync_NullCallback_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var configs = new[]
        {
            new McpConnectionConfigV1
            {
                Id = Guid.NewGuid(),
                Name = "server",
                Endpoint = "http://localhost:19996/mcp",
            },
        };

        // Should not throw even with null callback
        await _provider.InitializeAsync(
            configs,
            Array.Empty<McpStdioConnectionConfigV1>(),
            null,
            "test-user",
            _loggerMock.Object,
            null,
            cts.Token);
    }

    #endregion
}
