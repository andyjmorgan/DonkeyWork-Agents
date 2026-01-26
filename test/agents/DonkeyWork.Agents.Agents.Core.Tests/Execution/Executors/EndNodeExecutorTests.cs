using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Executors;

/// <summary>
/// Unit tests for EndNodeExecutor.
/// Tests final output collection and formatting.
/// </summary>
public class EndNodeExecutorTests
{
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteInternalAsync_WithUpstreamNodeOutput_ReturnsFinalOutput()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var upstreamOutput = new TestNodeOutput { Data = "test output" };
        context.NodeOutputs[upstreamNodeName] = upstreamOutput;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.FinalOutput);
        Assert.IsType<EndNodeOutput>(result);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithStringUpstreamOutput_ReturnsString()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var upstreamOutput = "simple string output";
        context.NodeOutputs[upstreamNodeName] = upstreamOutput;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(upstreamOutput, result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithNodeOutputType_CallsToMessageOutput()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var upstreamOutput = new TestNodeOutput { Data = "test data" };
        context.NodeOutputs[upstreamNodeName] = upstreamOutput;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var finalOutputString = result.FinalOutput as string;
        Assert.NotNull(finalOutputString);
        Assert.Contains("test data", finalOutputString);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithComplexObject_SerializesToJson()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var complexObject = new { property = "value", number = 42 };
        context.NodeOutputs[upstreamNodeName] = complexObject;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var finalOutputString = result.FinalOutput as string;
        Assert.NotNull(finalOutputString);
        Assert.Contains("property", finalOutputString);
        Assert.Contains("value", finalOutputString);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteInternalAsync_WithMissingUpstreamNode_ThrowsException()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();
        // Note: No upstream output added to context

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );

        Assert.Contains("could not find output from upstream node", exception.Message);
        Assert.Contains(upstreamNodeName, exception.Message);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithWrongUpstreamNodeName_ThrowsException()
    {
        // Arrange
        var expectedUpstreamName = "model_1";
        var actualUpstreamName = "model_2";
        var executor = new EndNodeExecutor(expectedUpstreamName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs[actualUpstreamName] = "output"; // Wrong node name

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );

        Assert.Contains("could not find output from upstream node", exception.Message);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithNullUpstreamOutput_ThrowsException()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs[upstreamNodeName] = null!; // Null output

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert - should handle null gracefully (serializes to "null" string)
        Assert.NotNull(result);
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public async Task ExecuteInternalAsync_OutputToMessageOutput_ReturnsString()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs[upstreamNodeName] = "test output";

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var messageOutput = result.ToMessageOutput();
        Assert.IsType<string>(messageOutput);
        Assert.Equal("test output", messageOutput);
    }

    [Fact]
    public async Task ExecuteInternalAsync_OutputToString_ReturnsReadableFormat()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs[upstreamNodeName] = "test output";

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var stringOutput = result.ToString();
        Assert.NotNull(stringOutput);
        Assert.Equal("test output", stringOutput);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithJsonObject_OutputToStringReturnsJson()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var jsonObject = new { result = "success", count = 5 };
        context.NodeOutputs[upstreamNodeName] = jsonObject;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var stringOutput = result.ToString();
        Assert.NotNull(stringOutput);
        Assert.Contains("result", stringOutput);
        Assert.Contains("success", stringOutput);
    }

    #endregion

    #region Multiple Upstream Scenarios

    [Fact]
    public async Task ExecuteInternalAsync_WithMultipleNodeOutputsInContext_UsesSpecifiedUpstream()
    {
        // Arrange
        var upstreamNodeName = "model_2";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs["model_1"] = "first output";
        context.NodeOutputs["model_2"] = "second output";
        context.NodeOutputs["model_3"] = "third output";

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("second output", result.FinalOutput);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteInternalAsync_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        context.NodeOutputs[upstreamNodeName] = "";

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithWhitespaceString_PreservesWhitespace()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var whitespace = "   ";
        context.NodeOutputs[upstreamNodeName] = whitespace;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(whitespace, result.FinalOutput);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithArrayOutput_SerializesCorrectly()
    {
        // Arrange
        var upstreamNodeName = "model_1";
        var executor = new EndNodeExecutor(upstreamNodeName);
        var config = new EndNodeConfiguration { Name = "end_1" };
        var context = CreateContext();

        var arrayOutput = new[] { "item1", "item2", "item3" };
        context.NodeOutputs[upstreamNodeName] = arrayOutput;

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var finalOutputString = result.FinalOutput as string;
        Assert.NotNull(finalOutputString);
        Assert.Contains("item1", finalOutputString);
        Assert.Contains("item2", finalOutputString);
    }

    #endregion

    #region Helper Methods

    private DonkeyWork.Agents.Agents.Core.Execution.ExecutionContext CreateContext()
    {
        return new DonkeyWork.Agents.Agents.Core.Execution.ExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            Input = new { },
            UserId = _testUserId
        };
    }

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
