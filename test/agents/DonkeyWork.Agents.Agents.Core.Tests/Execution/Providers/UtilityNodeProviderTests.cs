using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Providers;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Providers;

/// <summary>
/// Unit tests for UtilityNodeProvider.
/// Tests MessageFormatter node execution with Scriban templates.
/// </summary>
public class UtilityNodeProviderTests
{
    private readonly Mock<IExecutionContext> _executionContextMock;
    private readonly Mock<ILogger<UtilityNodeProvider>> _loggerMock;
    private readonly UtilityNodeProvider _provider;

    public UtilityNodeProviderTests()
    {
        _executionContextMock = new Mock<IExecutionContext>();
        _loggerMock = new Mock<ILogger<UtilityNodeProvider>>();

        _executionContextMock.Setup(c => c.ExecutionId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.Input).Returns(new { message = "Hello" });
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(new Dictionary<string, object>());

        _provider = new UtilityNodeProvider(_executionContextMock.Object, _loggerMock.Object);
    }

    #region ExecuteMessageFormatterAsync Tests

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithStaticTemplate_ReturnsFormattedMessage()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Hello, World!"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, World!", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithInputVariable_ResolvesVariable()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { name = "Claude" });

        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Hello, {{ input.name }}!"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("Hello, Claude!", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithExecutionIdVariable_ResolvesVariable()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        _executionContextMock.Setup(c => c.ExecutionId).Returns(executionId);

        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Execution: {{ execution_id }}"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal($"Execution: {executionId}", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithStepsVariable_ResolvesFromNodeOutputs()
    {
        // Arrange
        var nodeOutputs = new Dictionary<string, object>
        {
            ["previous_step"] = new { result = "success" }
        };
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);

        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Previous: {{ steps.previous_step.result }}"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("Previous: success", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithEmptyTemplate_ReturnsEmptyString()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = ""
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithInvalidTemplate_ThrowsException()
    {
        // Arrange - use an invalid expression that Scriban definitely reports as an error
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "{{ for x in items }}{{ end }}" // missing end without proper loop
        };

        // The template above is actually valid syntax, so let's use one with a syntax error
        config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "{{ 1 + }}" // incomplete expression
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None)
        );

        Assert.Contains("Template parsing errors", exception.Message);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithScribanLogic_ExecutesLogic()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { count = 5 });

        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "{{ if input.count > 3 }}Many{{ else }}Few{{ end }}"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("Many", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_OutputToMessageOutput_ReturnsFormattedMessage()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Test message"
        };

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);
        var messageOutput = result.ToMessageOutput();

        // Assert
        Assert.Equal("Test message", messageOutput);
    }

    #endregion
}
