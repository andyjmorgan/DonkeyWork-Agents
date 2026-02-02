using DonkeyWork.Agents.Mcp.Core.Services;
using DonkeyWork.Agents.Mcp.Core.Tests.TestTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Mcp.Core.Tests.Services;

public class McpToolRegistryTests
{
    private readonly Mock<ILogger<McpToolRegistry>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;

    public McpToolRegistryTests()
    {
        _loggerMock = new Mock<ILogger<McpToolRegistry>>();
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    #region Initialize Tests

    [Fact]
    public void Initialize_WithAssemblyContainingTools_DiscoversAllTools()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        var testAssembly = typeof(TestMcpTools).Assembly;

        // Act
        registry.Initialize(testAssembly);

        // Assert
        var tools = registry.GetAllTools();
        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.ProtocolTool.Name == "test_greeting");
        Assert.Contains(tools, t => t.ProtocolTool.Name == "test_add");
        Assert.Contains(tools, t => t.ProtocolTool.Name == "test_echo");
    }

    [Fact]
    public void Initialize_WithMultipleToolTypes_DiscoversToolsFromAllTypes()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        var testAssembly = typeof(TestMcpTools).Assembly;

        // Act
        registry.Initialize(testAssembly);

        // Assert
        var tools = registry.GetAllTools();

        // TestMcpTools has 3 tools, AnotherTestMcpTools has 2 tools
        Assert.True(tools.Count >= 5, $"Expected at least 5 tools, found {tools.Count}");
    }

    [Fact]
    public void Initialize_DoesNotDiscoverToolsFromNonMcpToolTypes()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        var testAssembly = typeof(TestMcpTools).Assembly;

        // Act
        registry.Initialize(testAssembly);

        // Assert
        var tools = registry.GetAllTools();
        Assert.DoesNotContain(tools, t => t.ProtocolTool.Name == "should_not_discover");
    }

    [Fact]
    public void Initialize_CalledMultipleTimes_OnlyInitializesOnce()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        var testAssembly = typeof(TestMcpTools).Assembly;

        // Act
        registry.Initialize(testAssembly);
        var firstCount = registry.GetAllTools().Count;
        registry.Initialize(testAssembly);
        var secondCount = registry.GetAllTools().Count;

        // Assert
        Assert.Equal(firstCount, secondCount);
    }

    #endregion

    #region GetTool Tests

    [Fact]
    public void GetTool_WithExistingToolName_ReturnsTool()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        registry.Initialize(typeof(TestMcpTools).Assembly);

        // Act
        var tool = registry.GetTool("test_greeting");

        // Assert
        Assert.NotNull(tool);
        Assert.Equal("test_greeting", tool.ProtocolTool.Name);
    }

    [Fact]
    public void GetTool_WithNonExistentToolName_ReturnsNull()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        registry.Initialize(typeof(TestMcpTools).Assembly);

        // Act
        var tool = registry.GetTool("non_existent_tool");

        // Assert
        Assert.Null(tool);
    }

    [Fact]
    public void GetTool_IsCaseSensitive()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        registry.Initialize(typeof(TestMcpTools).Assembly);

        // Act
        var tool = registry.GetTool("TEST_GREETING");

        // Assert
        Assert.Null(tool);
    }

    #endregion

    #region GetAllTools Tests

    [Fact]
    public void GetAllTools_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => registry.GetAllTools());
    }

    [Fact]
    public void GetAllTools_AfterInitialize_ReturnsReadOnlyList()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);
        registry.Initialize(typeof(TestMcpTools).Assembly);

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<ModelContextProtocol.Server.McpServerTool>>(tools);
    }

    #endregion

    #region Empty Assembly Tests

    [Fact]
    public void Initialize_WithEmptyAssemblyArray_InitializesWithNoTools()
    {
        // Arrange
        var registry = new McpToolRegistry(_loggerMock.Object, _serviceProvider);

        // Act
        registry.Initialize();

        // Assert
        var tools = registry.GetAllTools();
        Assert.Empty(tools);
    }

    #endregion
}
