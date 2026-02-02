using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Services;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Services;

/// <summary>
/// Unit tests for TemplateRenderer.
/// Tests Scriban template rendering with various input types.
/// </summary>
public class TemplateRendererTests
{
    private readonly Mock<IExecutionContext> _executionContextMock;

    public TemplateRendererTests()
    {
        _executionContextMock = new Mock<IExecutionContext>();
        _executionContextMock.Setup(c => c.ExecutionId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(new Dictionary<string, object>());
    }

    private TemplateRenderer CreateRenderer() => new TemplateRenderer(_executionContextMock.Object);

    #region RenderAsync - Basic Tests

    [Fact]
    public async Task RenderAsync_WithStaticTemplate_ReturnsUnchangedText()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Hello, World!");

        // Assert
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public async Task RenderAsync_WithEmptyTemplate_ReturnsEmptyString()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task RenderAsync_WithWhitespaceTemplate_ReturnsEmptyString()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("   ");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public async Task RenderAsync_WithNullTemplate_ReturnsEmptyString()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync(null!);

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region RenderAsync - Input Variable Tests

    [Fact]
    public async Task RenderAsync_WithPascalCaseInput_ResolvesVariable()
    {
        // Arrange - using anonymous object
        _executionContextMock.Setup(c => c.Input).Returns(new { Name = "Claude" });
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Hello, {{ Input.Name }}!");

        // Assert
        Assert.Equal("Hello, Claude!", result);
    }

    [Fact]
    public async Task RenderAsync_WithJsonElementInput_ResolvesVariable()
    {
        // Arrange - using JsonElement (common when input comes from JSON deserialization)
        var jsonInput = JsonDocument.Parse("""{"name": "Claude", "age": 30}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Hello, {{ Input.name }}! Age: {{ Input.age }}");

        // Assert
        Assert.Equal("Hello, Claude! Age: 30", result);
    }

    [Fact]
    public async Task RenderAsync_WithNestedJsonInput_ResolvesNestedProperties()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""
            {
                "user": {
                    "profile": {
                        "name": "Claude",
                        "email": "claude@example.com"
                    }
                }
            }
            """).RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Name: {{ Input.user.profile.name }}, Email: {{ Input.user.profile.email }}");

        // Assert
        Assert.Equal("Name: Claude, Email: claude@example.com", result);
    }

    [Fact]
    public async Task RenderAsync_WithArrayInput_ResolvesArrayAccess()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"items": ["apple", "banana", "cherry"]}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("First: {{ Input.items[0] }}, Second: {{ Input.items[1] }}");

        // Assert
        Assert.Equal("First: apple, Second: banana", result);
    }

    [Fact]
    public async Task RenderAsync_WithBooleanInput_ResolvesBooleans()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"active": true, "disabled": false}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Active: {{ Input.active }}, Disabled: {{ Input.disabled }}");

        // Assert
        Assert.Equal("Active: true, Disabled: false", result);
    }

    [Fact]
    public async Task RenderAsync_WithNullInputValue_HandlesNull()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"name": null}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Name: {{ Input.name }}");

        // Assert
        Assert.Equal("Name: ", result);
    }

    #endregion

    #region RenderAsync - Steps Variable Tests

    [Fact]
    public async Task RenderAsync_WithStepsVariable_ResolvesFromNodeOutputs()
    {
        // Arrange
        var nodeOutputs = new Dictionary<string, object>
        {
            ["previous_step"] = new { Result = "success", Count = 42 }
        };
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Previous: {{ Steps.previous_step.Result }}, Count: {{ Steps.previous_step.Count }}");

        // Assert
        Assert.Equal("Previous: success, Count: 42", result);
    }

    [Fact]
    public async Task RenderAsync_WithMultipleSteps_ResolvesAllSteps()
    {
        // Arrange
        var nodeOutputs = new Dictionary<string, object>
        {
            ["step_1"] = new { Value = "first" },
            ["step_2"] = new { Value = "second" }
        };
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        _executionContextMock.Setup(c => c.NodeOutputs).Returns(nodeOutputs);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Step1: {{ Steps.step_1.Value }}, Step2: {{ Steps.step_2.Value }}");

        // Assert
        Assert.Equal("Step1: first, Step2: second", result);
    }

    #endregion

    #region RenderAsync - Scriban Logic Tests

    [Fact]
    public async Task RenderAsync_WithIfCondition_ExecutesCondition()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"count": 5}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("{{ if Input.count > 3 }}Many{{ else }}Few{{ end }}");

        // Assert
        Assert.Equal("Many", result);
    }

    [Fact]
    public async Task RenderAsync_WithForLoop_ExecutesLoop()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"items": ["a", "b", "c"]}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("{{ for item in Input.items }}{{ item }},{{ end }}");

        // Assert
        Assert.Equal("a,b,c,", result);
    }

    [Fact]
    public async Task RenderAsync_WithStringFunctions_ExecutesFunctions()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"name": "claude"}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("{{ Input.name | string.upcase }}");

        // Assert
        Assert.Equal("CLAUDE", result);
    }

    #endregion

    #region RenderAsync - Error Handling Tests

    [Fact]
    public async Task RenderAsync_WithInvalidSyntax_ThrowsException()
    {
        // Arrange
        _executionContextMock.Setup(c => c.Input).Returns(new { });
        var renderer = CreateRenderer();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await renderer.RenderAsync("{{ 1 + }}")
        );

        Assert.Contains("Template parse error", exception.Message);
    }

    [Fact]
    public async Task RenderAsync_WithUndefinedVariable_ReturnsEmptyForMissingProperty()
    {
        // Arrange
        var jsonInput = JsonDocument.Parse("""{"name": "Claude"}""").RootElement;
        _executionContextMock.Setup(c => c.Input).Returns(jsonInput);
        var renderer = CreateRenderer();

        // Act
        var result = await renderer.RenderAsync("Value: {{ Input.nonexistent }}");

        // Assert
        Assert.Equal("Value: ", result);
    }

    [Fact]
    public async Task RenderAsync_WithNullInput_DoesNotThrowWhenAccessingProperties()
    {
        // Arrange - Input is null
        _executionContextMock.Setup(c => c.Input).Returns((object?)null);
        var renderer = CreateRenderer();

        // Act - Accessing a property on null Input should not throw
        var result = await renderer.RenderAsync("Value: {{ Input.something }}");

        // Assert - Should return empty for the missing property
        Assert.Equal("Value: ", result);
    }

    #endregion

}
