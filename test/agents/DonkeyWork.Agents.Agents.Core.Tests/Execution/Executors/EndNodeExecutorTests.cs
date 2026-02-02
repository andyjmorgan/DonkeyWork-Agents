using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Executors;

/// <summary>
/// Unit tests for EndNodeExecutor.
/// Tests final output collection and formatting.
/// </summary>
public class EndNodeExecutorTests
{
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _testExecutionId = Guid.NewGuid();
    private readonly Guid _testNodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Mock<IExecutionStreamWriter> _streamWriterMock;
    private readonly Mock<IExecutionContext> _contextMock;
    private readonly EndNodeExecutor _executor;

    public EndNodeExecutorTests()
    {
        _streamWriterMock = new Mock<IExecutionStreamWriter>();
        _contextMock = new Mock<IExecutionContext>();
        _contextMock.Setup(c => c.ExecutionId).Returns(_testExecutionId);
        _contextMock.Setup(c => c.UserId).Returns(_testUserId);
        _executor = new EndNodeExecutor(_streamWriterMock.Object, _contextMock.Object);
    }

    private static EndNodeConfiguration CreateConfig(string name = "end_1")
    {
        return new EndNodeConfiguration { Name = name };
    }

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithUpstreamNodeOutput_ReturnsFinalOutput()
    {
        // Arrange
        var config = CreateConfig();
        var upstreamOutput = new TestNodeOutput { Data = "test output" };
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = upstreamOutput };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<EndNodeOutput>(result);
        var endOutput = (EndNodeOutput)result;
        Assert.NotNull(endOutput.FinalOutput);
    }

    [Fact]
    public async Task ExecuteAsync_WithStringUpstreamOutput_ReturnsString()
    {
        // Arrange
        var config = CreateConfig();
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = "simple string output" };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var endOutput = (EndNodeOutput)result;
        Assert.Equal("simple string output", endOutput.FinalOutput);
    }

    [Fact]
    public async Task ExecuteAsync_WithNodeOutputType_CallsToMessageOutput()
    {
        // Arrange
        var config = CreateConfig();
        var upstreamOutput = new TestNodeOutput { Data = "test data" };
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = upstreamOutput };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var endOutput = (EndNodeOutput)result;
        var finalOutputString = endOutput.FinalOutput as string;
        Assert.NotNull(finalOutputString);
        Assert.Contains("test data", finalOutputString);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexObject_SerializesToJson()
    {
        // Arrange
        var config = CreateConfig();
        var complexObject = new { property = "value", number = 42 };
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = complexObject };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var endOutput = (EndNodeOutput)result;
        var finalOutputString = endOutput.FinalOutput as string;
        Assert.NotNull(finalOutputString);
        Assert.Contains("property", finalOutputString);
        Assert.Contains("value", finalOutputString);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithNoUpstreamNodes_ThrowsException()
    {
        // Arrange
        var config = CreateConfig();
        var nodeOutputs = new Dictionary<string, object>(); // Empty
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None)
        );

        Assert.Contains("no upstream outputs", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullUpstreamOutput_HandlesGracefully()
    {
        // Arrange
        var config = CreateConfig();
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = null! };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert - should handle null gracefully (serializes to "null" string)
        Assert.NotNull(result);
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public async Task ExecuteAsync_OutputToMessageOutput_ReturnsString()
    {
        // Arrange
        var config = CreateConfig();
        var nodeOutputs = new Dictionary<string, object> { ["model_1"] = "test output" };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        var endOutput = (EndNodeOutput)result;
        var messageOutput = endOutput.ToMessageOutput();
        Assert.IsType<string>(messageOutput);
        Assert.Equal("test output", messageOutput);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleNodeOutputsInContext_UsesLastOutput()
    {
        // Arrange
        var config = CreateConfig();
        var nodeOutputs = new Dictionary<string, object>
        {
            ["model_1"] = "first output",
            ["model_2"] = "second output",
            ["model_3"] = "third output"
        };
        _contextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        // Act
        var result = await _executor.ExecuteAsync(_testNodeId, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var endOutput = (EndNodeOutput)result;
        Assert.Equal("third output", endOutput.FinalOutput);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Test implementation of NodeOutput for testing purposes.
    /// </summary>
    private class TestNodeOutput : NodeOutput
    {
        public string Data { get; set; } = string.Empty;

        public override string ToMessageOutput()
        {
            return JsonSerializer.Serialize(new { data = Data });
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(new { data = Data });
        }
    }

    #endregion
}
