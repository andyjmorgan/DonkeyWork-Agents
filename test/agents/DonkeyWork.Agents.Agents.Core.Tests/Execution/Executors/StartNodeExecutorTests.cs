using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Executors;

/// <summary>
/// Unit tests for StartNodeExecutor.
/// StartNodeExecutor is a simple pass-through - it exposes the execution input.
/// </summary>
public class StartNodeExecutorTests
{
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _testExecutionId = Guid.NewGuid();
    private readonly Mock<IExecutionStreamWriter> _streamWriterMock;
    private readonly Mock<IExecutionContext> _contextMock;
    private readonly StartNodeExecutor _executor;
    private readonly JsonElement _defaultInputSchema;

    public StartNodeExecutorTests()
    {
        _streamWriterMock = new Mock<IExecutionStreamWriter>();
        _contextMock = new Mock<IExecutionContext>();
        _contextMock.Setup(c => c.ExecutionId).Returns(_testExecutionId);
        _contextMock.Setup(c => c.UserId).Returns(_testUserId);
        _executor = new StartNodeExecutor(_streamWriterMock.Object, _contextMock.Object);

        // Create a default input schema for tests
        _defaultInputSchema = JsonDocument.Parse(@"{
            ""type"": ""object"",
            ""properties"": {
                ""input"": { ""type"": ""string"" }
            }
        }").RootElement;
    }

    private StartNodeConfiguration CreateConfig(string name = "start_1")
    {
        return new StartNodeConfiguration
        {
            Name = name,
            InputSchema = _defaultInputSchema
        };
    }

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithInput_ReturnsInput()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test value" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StartNodeOutput>(result);
        var startOutput = (StartNodeOutput)result;
        Assert.NotNull(startOutput.Input);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexInput_ReturnsInput()
    {
        // Arrange
        var config = CreateConfig();
        var input = new
        {
            name = "John Doe",
            age = 30,
            tags = new[] { "developer", "tester" }
        };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StartNodeOutput>(result);
    }

    [Fact]
    public async Task ExecuteAsync_OutputMatchesInput()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test value" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        var startOutput = (StartNodeOutput)result;
        var inputJson = JsonSerializer.Serialize(input);
        var outputJson = JsonSerializer.Serialize(startOutput.Input);
        Assert.Equal(inputJson, outputJson);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyObject_ReturnsEmptyObject()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert - should succeed, validation is not the executor's job
        Assert.NotNull(result);
        var startOutput = (StartNodeOutput)result;
        Assert.NotNull(startOutput.Input);
    }

    #endregion

    #region Output Tests

    [Fact]
    public async Task ExecuteAsync_Output_CanBeSerializedToJson()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test value" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        var startOutput = (StartNodeOutput)result;
        var json = startOutput.ToMessageOutput();
        Assert.NotNull(json);
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public async Task ExecuteAsync_Output_ToStringReturnsJsonRepresentation()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test value" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        var startOutput = (StartNodeOutput)result;
        var stringOutput = startOutput.ToString();
        Assert.NotNull(stringOutput);
        Assert.Contains("input", stringOutput);
        Assert.Contains("test value", stringOutput);
    }

    #endregion

    #region Event Emission Tests

    [Fact]
    public async Task ExecuteAsync_EmitsNodeStartedEvent()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        _streamWriterMock.Verify(
            s => s.WriteEventAsync(It.IsAny<NodeStartedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsNodeCompletedEvent()
    {
        // Arrange
        var config = CreateConfig();
        var input = new { input = "test" };
        _contextMock.Setup(c => c.Input).Returns(input);

        // Act
        await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        _streamWriterMock.Verify(
            s => s.WriteEventAsync(It.IsAny<NodeCompletedEvent>()),
            Times.Once);
    }

    #endregion
}
