using System.ComponentModel;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Core;
using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools;

public class AgentToolRegistryTests
{
    private readonly Mock<ILogger<AgentToolRegistry>> _logger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAssembly_ScansToolsFromAssembly()
    {
        // Act
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Assert - should find tools from test types in this assembly
        Assert.True(registry.HasTool("test_tool_a"));
    }

    [Fact]
    public void Constructor_LogsToolCount()
    {
        // Act
        _ = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AgentToolRegistry initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region HasTool Tests

    [Fact]
    public void HasTool_WithExistingTool_ReturnsTrue()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act & Assert
        Assert.True(registry.HasTool("test_tool_a"));
    }

    [Fact]
    public void HasTool_WithNonExistentTool_ReturnsFalse()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act & Assert
        Assert.False(registry.HasTool("nonexistent_tool"));
    }

    [Fact]
    public void HasTool_IsCaseInsensitive()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act & Assert
        Assert.True(registry.HasTool("TEST_TOOL_A"));
    }

    #endregion

    #region GetDisplayName Tests

    [Fact]
    public void GetDisplayName_WithDisplayName_ReturnsDisplayName()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var displayName = registry.GetDisplayName("test_tool_display");

        // Assert
        Assert.Equal("Test Display", displayName);
    }

    [Fact]
    public void GetDisplayName_WithoutDisplayName_ReturnsNull()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var displayName = registry.GetDisplayName("test_tool_a");

        // Assert
        Assert.Null(displayName);
    }

    [Fact]
    public void GetDisplayName_WithNonExistentTool_ReturnsNull()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var displayName = registry.GetDisplayName("nonexistent");

        // Assert
        Assert.Null(displayName);
    }

    #endregion

    #region GetAllToolNames Tests

    [Fact]
    public void GetAllToolNames_ReturnsAllRegisteredTools()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var names = registry.GetAllToolNames();

        // Assert
        Assert.Contains("test_tool_a", names);
        Assert.Contains("test_tool_b", names);
    }

    #endregion

    #region GetToolDefinitions Tests

    [Fact]
    public void GetToolDefinitions_ForRegisteredType_ReturnsDefinitions()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var definitions = registry.GetToolDefinitions(typeof(TestToolsA));

        // Assert
        Assert.Single(definitions);
        Assert.Equal("test_tool_a", definitions[0].Name);
    }

    [Fact]
    public void GetToolDefinitions_ForMultipleTypes_ReturnsAll()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var definitions = registry.GetToolDefinitions(typeof(TestToolsA), typeof(TestToolsB));

        // Assert
        Assert.Equal(2, definitions.Count);
    }

    [Fact]
    public void GetToolDefinitions_ForUnregisteredType_ReturnsEmpty()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);

        // Act
        var definitions = registry.GetToolDefinitions(typeof(string));

        // Assert
        Assert.Empty(definitions);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithValidTool_ReturnsResult()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);
        var input = JsonSerializer.Deserialize<JsonElement>("""{"value": "hello"}""");
        var context = CreateGrainContext();

        // Act
        var result = await registry.ExecuteAsync("test_tool_a", input, context, CreateIdentityContext(), CreateServiceProvider(), CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("hello", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentTool_ReturnsError()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var context = CreateGrainContext();

        // Act
        var result = await registry.ExecuteAsync("nonexistent", input, context, CreateIdentityContext(), CreateServiceProvider(), CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeTypes_RejectsOutOfScope()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);
        var input = JsonSerializer.Deserialize<JsonElement>("""{"value": "test"}""");
        var context = CreateGrainContext();

        // Act - test_tool_a belongs to TestToolsA, scope is TestToolsB only
        var result = await registry.ExecuteAsync(
            "test_tool_a", input, context, CreateIdentityContext(), CreateServiceProvider(), CancellationToken.None,
            scopeTypes: [typeof(TestToolsB)]);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not available", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithScopeTypes_AllowsInScope()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(TestToolsA).Assembly);
        var input = JsonSerializer.Deserialize<JsonElement>("""{"value": "test"}""");
        var context = CreateGrainContext();

        // Act
        var result = await registry.ExecuteAsync(
            "test_tool_a", input, context, CreateIdentityContext(), CreateServiceProvider(), CancellationToken.None,
            scopeTypes: [typeof(TestToolsA)]);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolThrows_ReturnsError()
    {
        // Arrange
        var registry = new AgentToolRegistry(_logger.Object, typeof(ThrowingToolClass).Assembly);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var context = CreateGrainContext();

        // Act
        var result = await registry.ExecuteAsync("throwing_tool", input, context, CreateIdentityContext(), CreateServiceProvider(), CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Tool execution failed", result.Content);
    }

    #endregion

    #region Helper Types

    private static GrainContext CreateGrainContext()
    {
        return new GrainContext
        {
            GrainKey = "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222",
            ConversationId = "22222222-2222-2222-2222-222222222222",
            GrainFactory = new Mock<IGrainFactory>().Object,
            Logger = new Mock<Microsoft.Extensions.Logging.ILogger>().Object,
        };
    }

    private static IIdentityContext CreateIdentityContext()
    {
        var mock = new Mock<IIdentityContext>();
        mock.Setup(x => x.UserId).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        mock.Setup(x => x.IsAuthenticated).Returns(true);
        return mock.Object;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection().BuildServiceProvider();
    }

    #endregion
}

#region Test Tool Types (must be public for reflection)

public class TestToolsA
{
    [AgentTool("test_tool_a")]
    [Description("Test tool A")]
    public ToolResult RunA(string value) => ToolResult.Success(value);
}

public class TestToolsB
{
    [AgentTool("test_tool_b")]
    [Description("Test tool B")]
    public ToolResult RunB(string value) => ToolResult.Success(value);
}

public class TestToolsWithDisplay
{
    [AgentTool("test_tool_display", DisplayName = "Test Display")]
    [Description("Tool with display name")]
    public ToolResult Run() => ToolResult.Success("done");
}

public class ThrowingToolClass
{
    [AgentTool("throwing_tool")]
    [Description("A tool that throws")]
    public ToolResult Run() => throw new InvalidOperationException("test error");
}

#endregion
