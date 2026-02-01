using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Actions.Core.Services;

namespace DonkeyWork.Agents.Actions.Core.Tests.Services;

public class ActionSchemaServiceTests
{
    private readonly ActionSchemaService _service = new();

    // Test parameter class
    [ActionNode(
        actionType: "test_action",
        category: "Testing",
        Group = "Unit Tests",
        Icon = "test-icon",
        Description = "Test action for unit tests",
        DisplayName = "Test Action",
        MaxInputs = 1,
        MaxOutputs = 2,
        Enabled = true)]
    private class TestActionParameters : BaseActionParameters
    {
        [Required]
        [Display(Name = "Name", Description = "User name")]
        [SupportVariables]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100)]
        [Slider(Step = 1)]
        [DefaultValue(50)]
        public Resolvable<int> Count { get; set; } = 50;

        [EditorType(EditorType.Code)]
        public string? Code { get; set; }

        public TestEnum EnumValue { get; set; }

        public override (bool valid, List<ValidationResult> results) IsValid()
        {
            return ValidateDataAnnotations();
        }
    }

    private enum TestEnum
    {
        Option1,
        Option2,
        Option3
    }

    [Fact]
    public void GenerateSchema_WithActionNodeAttribute_ReturnsSchema()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("test_action", schema.ActionType);
        Assert.Equal("Test Action", schema.DisplayName);
        Assert.Equal("Testing", schema.Category);
        Assert.Equal("Unit Tests", schema.Group);
        Assert.Equal("test-icon", schema.Icon);
        Assert.Equal("Test action for unit tests", schema.Description);
        Assert.Equal(1, schema.MaxInputs);
        Assert.Equal(2, schema.MaxOutputs);
        Assert.True(schema.Enabled);
    }

    [Fact]
    public void GenerateSchema_WithoutActionNodeAttribute_ThrowsException()
    {
        // Arrange
        var type = typeof(string);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GenerateSchema(type));
        Assert.Contains("does not have [ActionNode] attribute", exception.Message);
    }

    [Fact]
    public void GenerateSchema_IncludesAllParameters()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);

        // Assert
        Assert.Equal(4, schema.Parameters.Count);
        Assert.Contains(schema.Parameters, p => p.Name == "Name");
        Assert.Contains(schema.Parameters, p => p.Name == "Count");
        Assert.Contains(schema.Parameters, p => p.Name == "Code");
        Assert.Contains(schema.Parameters, p => p.Name == "EnumValue");
    }

    [Fact]
    public void GenerateSchema_RequiredParameter_HasRequiredFlag()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var nameParam = schema.Parameters.First(p => p.Name == "Name");

        // Assert
        Assert.True(nameParam.Required);
    }

    [Fact]
    public void GenerateSchema_DisplayAttribute_MapsCorrectly()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var nameParam = schema.Parameters.First(p => p.Name == "Name");

        // Assert
        Assert.Equal("Name", nameParam.DisplayName);
        Assert.Equal("User name", nameParam.Description);
    }

    [Fact]
    public void GenerateSchema_SupportVariablesAttribute_SetsFlag()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var nameParam = schema.Parameters.First(p => p.Name == "Name");

        // Assert
        Assert.True(nameParam.SupportsVariables);
    }

    [Fact]
    public void GenerateSchema_ResolvableType_SetsResolvableFlag()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var countParam = schema.Parameters.First(p => p.Name == "Count");

        // Assert
        Assert.True(countParam.Resolvable);
        Assert.Equal("number", countParam.Type);
    }

    [Fact]
    public void GenerateSchema_SliderAttribute_SetsControlType()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var countParam = schema.Parameters.First(p => p.Name == "Count");

        // Assert
        Assert.Equal("slider", countParam.ControlType);
    }

    [Fact]
    public void GenerateSchema_RangeAttribute_SetsValidation()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var countParam = schema.Parameters.First(p => p.Name == "Count");

        // Assert
        Assert.NotNull(countParam.Validation);
        Assert.Equal(1, countParam.Validation.Min);
        Assert.Equal(100, countParam.Validation.Max);
        Assert.Equal(1, countParam.Validation.Step);
    }

    [Fact]
    public void GenerateSchema_DefaultValueAttribute_SetsDefaultValue()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var countParam = schema.Parameters.First(p => p.Name == "Count");

        // Assert
        Assert.Equal("50", countParam.DefaultValue);
    }

    [Fact]
    public void GenerateSchema_EditorTypeAttribute_SetsEditorType()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var codeParam = schema.Parameters.First(p => p.Name == "Code");

        // Assert
        Assert.Equal("Code", codeParam.EditorType);
        Assert.Equal("code", codeParam.ControlType);
    }

    [Fact]
    public void GenerateSchema_EnumType_GeneratesOptions()
    {
        // Arrange
        var type = typeof(TestActionParameters);

        // Act
        var schema = _service.GenerateSchema(type);
        var enumParam = schema.Parameters.First(p => p.Name == "EnumValue");

        // Assert
        Assert.Equal("enum", enumParam.Type);
        Assert.Equal("dropdown", enumParam.ControlType);
        Assert.NotNull(enumParam.Options);
        Assert.Equal(3, enumParam.Options.Count);
        Assert.Contains(enumParam.Options, o => o.Label == "Option1" && o.Value == "Option1");
        Assert.Contains(enumParam.Options, o => o.Label == "Option2" && o.Value == "Option2");
        Assert.Contains(enumParam.Options, o => o.Label == "Option3" && o.Value == "Option3");
    }

    [Fact]
    public void GenerateSchemas_ScansAssembly_FindsAllActionNodes()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        var schemas = _service.GenerateSchemas(assembly);

        // Assert
        Assert.Single(schemas);
        Assert.Equal("test_action", schemas[0].ActionType);
    }

    [Fact]
    public void ExportAsJson_GeneratesValidJson()
    {
        // Arrange
        var type = typeof(TestActionParameters);
        var schema = _service.GenerateSchema(type);
        var schemas = new List<DonkeyWork.Agents.Actions.Contracts.Models.Schema.ActionNodeSchema> { schema };

        // Act
        var json = _service.ExportAsJson(schemas);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("test_action", json);
        Assert.Contains("Test Action", json);
        Assert.Contains("actionType", json);
        Assert.Contains("parameters", json);
    }

    [Fact]
    public void ExportAsJson_UsesCamelCase()
    {
        // Arrange
        var type = typeof(TestActionParameters);
        var schema = _service.GenerateSchema(type);
        var schemas = new List<DonkeyWork.Agents.Actions.Contracts.Models.Schema.ActionNodeSchema> { schema };

        // Act
        var json = _service.ExportAsJson(schemas);

        // Assert
        Assert.Contains("\"actionType\":", json);
        Assert.Contains("\"displayName\":", json);
        Assert.DoesNotContain("\"ActionType\":", json);
        Assert.DoesNotContain("\"DisplayName\":", json);
    }
}
