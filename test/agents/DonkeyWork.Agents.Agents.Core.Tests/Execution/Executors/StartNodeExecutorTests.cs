using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Executors;

/// <summary>
/// Unit tests for StartNodeExecutor.
/// Tests input validation and execution flow.
/// </summary>
public class StartNodeExecutorTests
{
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _testExecutionId = Guid.NewGuid();
    private readonly Mock<ILogger<StartNodeExecutor>> _loggerMock;
    private readonly Mock<IExecutionStreamWriter> _streamWriterMock;
    private readonly Mock<IExecutionContext> _contextMock;
    private readonly StartNodeExecutor _executor;

    public StartNodeExecutorTests()
    {
        _loggerMock = new Mock<ILogger<StartNodeExecutor>>();
        _streamWriterMock = new Mock<IExecutionStreamWriter>();
        _contextMock = new Mock<IExecutionContext>();
        _contextMock.Setup(c => c.ExecutionId).Returns(_testExecutionId);
        _contextMock.Setup(c => c.UserId).Returns(_testUserId);
        _executor = new StartNodeExecutor(_streamWriterMock.Object, _contextMock.Object, _loggerMock.Object);
    }

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsInput()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        SetupContext(input, inputSchema);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StartNodeOutput>(result);
        var startOutput = (StartNodeOutput)result;
        Assert.NotNull(startOutput.Input);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexValidInput_ReturnsInput()
    {
        // Arrange
        var inputSchema = CreateComplexInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new
        {
            name = "John Doe",
            age = 30,
            tags = new[] { "developer", "tester" }
        };
        SetupContext(input, inputSchema);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<StartNodeOutput>(result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_OutputMatchesInput()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        SetupContext(input, inputSchema);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        var startOutput = (StartNodeOutput)result;
        var inputJson = JsonSerializer.Serialize(input);
        var outputJson = JsonSerializer.Serialize(startOutput.Input);
        Assert.Equal(inputJson, outputJson);
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task ExecuteAsync_WithMissingRequiredField_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Requires "input" field
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { wrongField = "test" }; // Missing required field
        SetupContext(input, inputSchema);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync("start_1", config, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongDataType_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Expects "input" as string
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = 123 }; // Wrong type (number instead of string)
        SetupContext(input, inputSchema);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync("start_1", config, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyInput_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Requires "input" field
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { }; // Empty object
        SetupContext(input, inputSchema);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync("start_1", config, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithAdditionalProperties_ValidatesSuccessfully()
    {
        // Arrange - schema doesn't specify additionalProperties: false
        var inputSchema = CreateBasicInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new
        {
            input = "test value",
            extraField = "extra" // Additional property
        };
        SetupContext(input, inputSchema);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert - should not throw
        Assert.NotNull(result);
    }

    #endregion

    #region Schema Tests

    [Fact]
    public async Task ExecuteAsync_WithInvalidJsonSchema_ThrowsException()
    {
        // Arrange
        var invalidSchema = "{ invalid json }";
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test" };
        SetupContext(input, invalidSchema);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync("start_1", config, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithMinimumConstraint_ValidInput_Succeeds()
    {
        // Arrange
        var inputSchema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""age"": { ""type"": ""integer"", ""minimum"": 18 }
            },
            ""required"": [""age""]
        }";
        var config = new StartNodeConfiguration { Name = "start_1" };
        var validInput = new { age = 25 };
        SetupContext(validInput, inputSchema);

        // Act
        var result = await _executor.ExecuteAsync("start_1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithMinimumConstraint_InvalidInput_ThrowsException()
    {
        // Arrange
        var inputSchema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""age"": { ""type"": ""integer"", ""minimum"": 18 }
            },
            ""required"": [""age""]
        }";
        var config = new StartNodeConfiguration { Name = "start_1" };
        var invalidInput = new { age = 15 };
        SetupContext(invalidInput, inputSchema);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _executor.ExecuteAsync("start_1", config, CancellationToken.None)
        );
    }

    #endregion

    #region Output Tests

    [Fact]
    public async Task ExecuteAsync_Output_CanBeSerializedToJson()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        SetupContext(input, inputSchema);

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
        var inputSchema = CreateBasicInputSchema();
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        SetupContext(input, inputSchema);

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

    #region Helper Methods

    private void SetupContext(object input, string inputSchema)
    {
        _contextMock.Setup(c => c.Input).Returns(input);
        _contextMock.Setup(c => c.InputSchema).Returns(inputSchema);
    }

    private string CreateBasicInputSchema()
    {
        return @"{
            ""type"": ""object"",
            ""properties"": {
                ""input"": { ""type"": ""string"" }
            },
            ""required"": [""input""]
        }";
    }

    private string CreateComplexInputSchema()
    {
        return @"{
            ""type"": ""object"",
            ""properties"": {
                ""name"": { ""type"": ""string"" },
                ""age"": { ""type"": ""integer"" },
                ""tags"": {
                    ""type"": ""array"",
                    ""items"": { ""type"": ""string"" }
                }
            },
            ""required"": [""name"", ""age""]
        }";
    }

    #endregion
}
