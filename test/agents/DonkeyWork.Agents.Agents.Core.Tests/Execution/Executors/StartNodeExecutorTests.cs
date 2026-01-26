using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Executors;

/// <summary>
/// Unit tests for StartNodeExecutor.
/// Tests input validation and execution flow.
/// </summary>
public class StartNodeExecutorTests
{
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteInternalAsync_WithValidInput_ReturnsInput()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Input);
        Assert.IsType<StartNodeOutput>(result);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithComplexValidInput_ReturnsInput()
    {
        // Arrange
        var inputSchema = CreateComplexInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new
        {
            name = "John Doe",
            age = 30,
            tags = new[] { "developer", "tester" }
        };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Input);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ValidInput_OutputMatchesInput()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var inputJson = JsonSerializer.Serialize(input);
        var outputJson = JsonSerializer.Serialize(result.Input);
        Assert.Equal(inputJson, outputJson);
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task ExecuteInternalAsync_WithMissingRequiredField_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Requires "input" field
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { wrongField = "test" }; // Missing required field
        var context = CreateContext(input);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.Message);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithWrongDataType_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Expects "input" as string
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = 123 }; // Wrong type (number instead of string)
        var context = CreateContext(input);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.Message);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithEmptyInput_ThrowsException()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema(); // Requires "input" field
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { }; // Empty object
        var context = CreateContext(input);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );

        Assert.Contains("Input validation failed", exception.Message);
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithAdditionalProperties_ValidatesSuccessfully()
    {
        // Arrange - schema doesn't specify additionalProperties: false
        var inputSchema = CreateBasicInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new
        {
            input = "test value",
            extraField = "extra" // Additional property
        };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert - should not throw
        Assert.NotNull(result);
    }

    #endregion

    #region Schema Tests

    [Fact]
    public async Task ExecuteInternalAsync_WithInvalidJsonSchema_ThrowsException()
    {
        // Arrange
        var invalidSchema = "{ invalid json }";
        var executor = new StartNodeExecutor(invalidSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test" };
        var context = CreateContext(input);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await executor.ExecuteInternalAsync(config, context, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExecuteInternalAsync_WithMinimumConstraint_ValidatesCorrectly()
    {
        // Arrange
        var inputSchema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""age"": { ""type"": ""integer"", ""minimum"": 18 }
            },
            ""required"": [""age""]
        }";
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };

        // Act & Assert - valid input
        var validInput = new { age = 25 };
        var validContext = CreateContext(validInput);
        var result = await executor.ExecuteInternalAsync(config, validContext, CancellationToken.None);
        Assert.NotNull(result);

        // Act & Assert - invalid input
        var invalidInput = new { age = 15 };
        var invalidContext = CreateContext(invalidInput);
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteInternalAsync(config, invalidContext, CancellationToken.None)
        );
    }

    #endregion

    #region Output Tests

    [Fact]
    public async Task ExecuteInternalAsync_Output_CanBeSerializedToJson()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var json = result.ToMessageOutput();
        Assert.NotNull(json);
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public async Task ExecuteInternalAsync_Output_ToStringReturnsJsonRepresentation()
    {
        // Arrange
        var inputSchema = CreateBasicInputSchema();
        var executor = new StartNodeExecutor(inputSchema);
        var config = new StartNodeConfiguration { Name = "start_1" };
        var input = new { input = "test value" };
        var context = CreateContext(input);

        // Act
        var result = await executor.ExecuteInternalAsync(config, context, CancellationToken.None);

        // Assert
        var stringOutput = result.ToString();
        Assert.NotNull(stringOutput);
        Assert.Contains("input", stringOutput);
        Assert.Contains("test value", stringOutput);
    }

    #endregion

    #region Helper Methods

    private DonkeyWork.Agents.Agents.Core.Execution.ExecutionContext CreateContext(object input)
    {
        return new DonkeyWork.Agents.Agents.Core.Execution.ExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            Input = input,
            UserId = _testUserId
        };
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
