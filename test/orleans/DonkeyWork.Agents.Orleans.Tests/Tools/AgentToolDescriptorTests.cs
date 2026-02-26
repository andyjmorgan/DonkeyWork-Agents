using System.ComponentModel;
using System.Text.Json;
using DonkeyWork.Agents.Orleans.Core;
using DonkeyWork.Agents.Orleans.Core.Tools;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Tools;

public class AgentToolDescriptorTests
{
    #region FromType Tests

    [Fact]
    public void FromType_WithNoToolAttributes_ReturnsEmptyList()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(NoToolMethods));

        // Assert
        Assert.Empty(descriptors);
    }

    [Fact]
    public void FromType_WithToolAttribute_ReturnsDescriptor()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        Assert.Single(descriptors);
        Assert.Equal("simple_tool", descriptors[0].Name);
    }

    [Fact]
    public void FromType_WithMultipleTools_ReturnsAll()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(MultiToolClass));

        // Assert
        Assert.Equal(2, descriptors.Count);
    }

    [Fact]
    public void FromType_UsesAttributeNameOverMethodName()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        Assert.Equal("simple_tool", descriptors[0].Name);
    }

    [Fact]
    public void FromType_WithoutAttributeName_UsesMethodName()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(NoNameToolClass));

        // Assert
        Assert.Equal("DoWork", descriptors[0].Name);
    }

    [Fact]
    public void FromType_ExtractsDescription()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        Assert.Equal("A simple test tool", descriptors[0].Description);
    }

    [Fact]
    public void FromType_WithDisplayName_SetsDisplayName()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(DisplayNameToolClass));

        // Assert
        Assert.Equal("My Display Name", descriptors[0].DisplayName);
    }

    [Fact]
    public void FromType_SkipsGrainContextParameter()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        Assert.DoesNotContain(descriptors[0].Parameters, p => p.ClrType == typeof(GrainContext));
    }

    [Fact]
    public void FromType_SkipsCancellationTokenParameter()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        Assert.DoesNotContain(descriptors[0].Parameters, p => p.ClrType == typeof(CancellationToken));
    }

    [Fact]
    public void FromType_ExtractsStringParameter()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("query", param.Name);
        Assert.Equal("string", param.JsonType);
        Assert.True(param.IsRequired);
    }

    [Fact]
    public void FromType_WithOptionalParameter_SetsNotRequired()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(OptionalParamToolClass));

        // Assert
        var timeoutParam = Assert.Single(descriptors[0].Parameters, p => p.Name == "timeout_seconds");
        Assert.False(timeoutParam.IsRequired);
    }

    [Fact]
    public void FromType_WithEnumParameter_SetsAllowedValues()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(EnumParamToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.NotNull(param.AllowedValues);
        Assert.Contains("ValueA", param.AllowedValues);
        Assert.Contains("ValueB", param.AllowedValues);
    }

    [Fact]
    public void FromType_WithParameterDescription_SetsDescription()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("The search query", param.Description);
    }

    [Fact]
    public void FromType_DiscoversBothStaticAndInstanceMethods()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(MixedToolClass));

        // Assert
        Assert.Equal(2, descriptors.Count);
        Assert.Contains(descriptors, d => d.Name == "instance_tool");
        Assert.Contains(descriptors, d => d.Name == "static_tool");
    }

    #endregion

    #region ToToolDefinition Tests

    [Fact]
    public void ToToolDefinition_ReturnsCorrectName()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Act
        var definition = descriptors[0].ToToolDefinition();

        // Assert
        Assert.Equal("simple_tool", definition.Name);
    }

    [Fact]
    public void ToToolDefinition_ReturnsCorrectDescription()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Act
        var definition = descriptors[0].ToToolDefinition();

        // Assert
        Assert.Equal("A simple test tool", definition.Description);
    }

    [Fact]
    public void ToToolDefinition_BuildsJsonSchema()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Act
        var definition = descriptors[0].ToToolDefinition();

        // Assert
        Assert.NotNull(definition.InputSchema);
        var schema = (Dictionary<string, object>)definition.InputSchema;
        Assert.Equal("object", schema["type"]);
        Assert.Contains("properties", schema.Keys);
    }

    [Fact]
    public void ToToolDefinition_IncludesRequiredParameters()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(SimpleToolClass));

        // Act
        var definition = descriptors[0].ToToolDefinition();

        // Assert
        var schema = (Dictionary<string, object>)definition.InputSchema!;
        var required = (List<string>)schema["required"];
        Assert.Contains("query", required);
    }

    [Fact]
    public void ToToolDefinition_WithNoRequiredParams_OmitsRequired()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(AllOptionalToolClass));

        // Act
        var definition = descriptors[0].ToToolDefinition();

        // Assert
        var schema = (Dictionary<string, object>)definition.InputSchema!;
        Assert.DoesNotContain("required", schema.Keys);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithStaticMethod_InvokesSuccessfully()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(StaticToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("""{"value": "test"}""");
        var context = CreateGrainContext();

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("static:test", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithInstanceMethod_InvokesSuccessfully()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(InstanceToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("""{"value": "hello"}""");
        var context = CreateGrainContext();

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal("instance:hello", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_InjectsGrainContext()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(ContextToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var context = CreateGrainContext();
        context.UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithOptionalParam_UsesDefault()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(OptionalParamToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var context = CreateGrainContext();

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.Equal("120", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithEnumParam_ParsesCorrectly()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(EnumParamToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("""{"choice": "ValueB"}""");
        var context = CreateGrainContext();

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.Equal("ValueB", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithIntParam_DeserializesCorrectly()
    {
        // Arrange
        var descriptors = AgentToolDescriptor.FromType(typeof(IntParamToolClass));
        var input = JsonSerializer.Deserialize<JsonElement>("""{"count": 42}""");
        var context = CreateGrainContext();

        // Act
        var result = await descriptors[0].ExecuteAsync(input, context, CancellationToken.None);

        // Assert
        Assert.Equal("42", result.Content);
    }

    #endregion

    #region Type Mapping Tests

    [Fact]
    public void FromType_MapsBoolToBoolean()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(BoolParamToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("boolean", param.JsonType);
    }

    [Fact]
    public void FromType_MapsIntToNumber()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(IntParamToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("number", param.JsonType);
    }

    [Fact]
    public void FromType_MapsGuidToString()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(GuidParamToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("string", param.JsonType);
    }

    [Fact]
    public void FromType_MapsEnumToString()
    {
        // Act
        var descriptors = AgentToolDescriptor.FromType(typeof(EnumParamToolClass));

        // Assert
        var param = Assert.Single(descriptors[0].Parameters);
        Assert.Equal("string", param.JsonType);
    }

    #endregion

    #region Helper Types

    private static GrainContext CreateGrainContext()
    {
        return new GrainContext
        {
            GrainKey = "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222",
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConversationId = "22222222-2222-2222-2222-222222222222",
            GrainFactory = new Mock<IGrainFactory>().Object,
            Logger = new Mock<Microsoft.Extensions.Logging.ILogger>().Object,
        };
    }

    public class NoToolMethods
    {
        public void SomeMethod() { }
    }

    public class SimpleToolClass
    {
        [AgentTool("simple_tool")]
        [Description("A simple test tool")]
        public Task<ToolResult> RunTool(
            [Description("The search query")] string query,
            GrainContext context,
            CancellationToken ct)
        {
            return Task.FromResult(ToolResult.Success(query));
        }
    }

    public class MultiToolClass
    {
        [AgentTool("tool_a")]
        [Description("Tool A")]
        public ToolResult ToolA(string input) => ToolResult.Success(input);

        [AgentTool("tool_b")]
        [Description("Tool B")]
        public ToolResult ToolB(string input) => ToolResult.Success(input);
    }

    public class NoNameToolClass
    {
        [AgentTool]
        [Description("A tool without explicit name")]
        public ToolResult DoWork() => ToolResult.Success("done");
    }

    public class DisplayNameToolClass
    {
        [AgentTool("display_tool", DisplayName = "My Display Name")]
        [Description("Tool with display name")]
        public ToolResult Run() => ToolResult.Success("done");
    }

    public class OptionalParamToolClass
    {
        [AgentTool("optional_tool")]
        [Description("Tool with optional param")]
        public ToolResult Run(int timeout_seconds = 120) => ToolResult.Success(timeout_seconds.ToString());
    }

    public class AllOptionalToolClass
    {
        [AgentTool("all_optional")]
        [Description("All optional params")]
        public ToolResult Run(string? name = null, int count = 5) => ToolResult.Success("ok");
    }

    public enum TestEnum { ValueA, ValueB }

    public class EnumParamToolClass
    {
        [AgentTool("enum_tool")]
        [Description("Tool with enum param")]
        public ToolResult Run(TestEnum choice) => ToolResult.Success(choice.ToString());
    }

    public class IntParamToolClass
    {
        [AgentTool("int_tool")]
        [Description("Tool with int param")]
        public ToolResult Run(int count) => ToolResult.Success(count.ToString());
    }

    public class BoolParamToolClass
    {
        [AgentTool("bool_tool")]
        [Description("Tool with bool param")]
        public ToolResult Run(bool flag) => ToolResult.Success(flag.ToString());
    }

    public class GuidParamToolClass
    {
        [AgentTool("guid_tool")]
        [Description("Tool with guid param")]
        public ToolResult Run(Guid id) => ToolResult.Success(id.ToString());
    }

    public class StaticToolClass
    {
        [AgentTool("static_tool")]
        [Description("A static tool")]
        public static ToolResult Run(string value) => ToolResult.Success($"static:{value}");
    }

    public class InstanceToolClass
    {
        [AgentTool("instance_tool")]
        [Description("An instance tool")]
        public ToolResult Run(string value) => ToolResult.Success($"instance:{value}");
    }

    public class ContextToolClass
    {
        [AgentTool("context_tool")]
        [Description("Tool that uses context")]
        public ToolResult Run(GrainContext context) => ToolResult.Success(context.UserId.ToString());
    }

    public class MixedToolClass
    {
        [AgentTool("instance_tool")]
        [Description("Instance method")]
        public ToolResult InstanceMethod() => ToolResult.Success("instance");

        [AgentTool("static_tool")]
        [Description("Static method")]
        public static ToolResult StaticMethod() => ToolResult.Success("static");
    }

    #endregion
}
