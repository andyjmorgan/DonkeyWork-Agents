using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution.Providers;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Providers;

/// <summary>
/// Unit tests for UtilityNodeProvider.
/// Tests MessageFormatter node execution with template rendering.
/// </summary>
public class UtilityNodeProviderTests
{
    private readonly Mock<ITemplateRenderer> _templateRendererMock;
    private readonly Mock<ILogger<UtilityNodeProvider>> _loggerMock;
    private readonly UtilityNodeProvider _provider;

    public UtilityNodeProviderTests()
    {
        _templateRendererMock = new Mock<ITemplateRenderer>();
        _loggerMock = new Mock<ILogger<UtilityNodeProvider>>();

        _provider = new UtilityNodeProvider(_templateRendererMock.Object, _loggerMock.Object);
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

        _templateRendererMock
            .Setup(r => r.RenderAsync("Hello, World!", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello, World!");

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello, World!", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithTemplateVariable_CallsRenderer()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Hello, {{ Input.name }}!"
        };

        _templateRendererMock
            .Setup(r => r.RenderAsync("Hello, {{ Input.name }}!", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello, Claude!");

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("Hello, Claude!", result.FormattedMessage);
        _templateRendererMock.Verify(
            r => r.RenderAsync("Hello, {{ Input.name }}!", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithStepsVariable_CallsRenderer()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Previous: {{ Steps.previous_step.Result }}"
        };

        _templateRendererMock
            .Setup(r => r.RenderAsync("Previous: {{ Steps.previous_step.Result }}", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Previous: success");

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

        _templateRendererMock
            .Setup(r => r.RenderAsync("", It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);

        // Assert
        Assert.Equal("", result.FormattedMessage);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WhenRendererThrows_PropagatesException()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "{{ 1 + }}"
        };

        _templateRendererMock
            .Setup(r => r.RenderAsync("{{ 1 + }}", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Template parse error: unexpected end of expression"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None)
        );

        Assert.Contains("Template parse error", exception.Message);
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

        _templateRendererMock
            .Setup(r => r.RenderAsync("Test message", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test message");

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, CancellationToken.None);
        var messageOutput = result.ToMessageOutput();

        // Assert
        Assert.Equal("Test message", messageOutput);
    }

    [Fact]
    public async Task ExecuteMessageFormatterAsync_WithCancellationToken_PassesTokenToRenderer()
    {
        // Arrange
        var config = new MessageFormatterNodeConfiguration
        {
            Name = "formatter_1",
            Template = "Hello"
        };

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _templateRendererMock
            .Setup(r => r.RenderAsync("Hello", token))
            .ReturnsAsync("Hello");

        // Act
        var result = await _provider.ExecuteMessageFormatterAsync(config, token);

        // Assert
        _templateRendererMock.Verify(r => r.RenderAsync("Hello", token), Times.Once);
    }

    #endregion
}
