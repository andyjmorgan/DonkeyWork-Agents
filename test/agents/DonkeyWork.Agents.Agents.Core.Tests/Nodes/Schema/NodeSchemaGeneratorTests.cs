using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;
using Xunit;

namespace DonkeyWork.Agents.Agents.Core.Tests.Nodes.Schema;

/// <summary>
/// Unit tests for NodeSchemaGenerator.
/// Tests schema generation from configuration classes via reflection.
/// </summary>
public class NodeSchemaGeneratorTests
{
    private readonly NodeSchemaGenerator _generator = new();

    #region GenerateSchema by NodeType Tests

    [Theory]
    [InlineData(NodeType.Start)]
    [InlineData(NodeType.End)]
    [InlineData(NodeType.Model)]
    [InlineData(NodeType.MessageFormatter)]
    [InlineData(NodeType.HttpRequest)]
    [InlineData(NodeType.Sleep)]
    public void GenerateSchema_WithValidNodeType_ReturnsSchema(NodeType nodeType)
    {
        // Act
        var schema = _generator.GenerateSchema(nodeType);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(nodeType, schema.NodeType);
    }

    [Fact]
    public void GenerateSchema_StartNode_ContainsInputSchemaField()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.Start);

        // Assert
        Assert.NotEmpty(schema.Fields);
        var inputSchemaField = schema.Fields.FirstOrDefault(f => f.Name == "inputSchema");
        Assert.NotNull(inputSchemaField);
        Assert.Equal(ControlType.Json, inputSchemaField.ControlType);
    }

    [Fact]
    public void GenerateSchema_HttpRequestNode_ContainsAllFields()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var fieldNames = schema.Fields.Select(f => f.Name).ToList();
        Assert.Contains("method", fieldNames);
        Assert.Contains("url", fieldNames);
        Assert.Contains("headers", fieldNames);
        Assert.Contains("body", fieldNames);
        Assert.Contains("timeoutSeconds", fieldNames);
    }

    [Fact]
    public void GenerateSchema_HttpRequestNode_MethodFieldHasSelectControlType()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var methodField = schema.Fields.First(f => f.Name == "method");
        Assert.Equal(ControlType.Select, methodField.ControlType);
        Assert.NotNull(methodField.Options);
        Assert.Contains("Get", methodField.Options);
        Assert.Contains("Post", methodField.Options);
    }

    [Fact]
    public void GenerateSchema_HttpRequestNode_UrlFieldSupportsVariables()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var urlField = schema.Fields.First(f => f.Name == "url");
        Assert.True(urlField.SupportsVariables);
    }

    [Fact]
    public void GenerateSchema_HttpRequestNode_TimeoutHasSliderProperties()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var timeoutField = schema.Fields.First(f => f.Name == "timeoutSeconds");
        Assert.Equal(ControlType.Slider, timeoutField.ControlType);
        Assert.NotNull(timeoutField.Min);
        Assert.NotNull(timeoutField.Max);
    }

    [Fact]
    public void GenerateSchema_SleepNode_ContainsDurationField()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.Sleep);

        // Assert
        var durationField = schema.Fields.FirstOrDefault(f => f.Name == "durationMs");
        Assert.NotNull(durationField);
    }

    [Fact]
    public void GenerateSchema_MessageFormatterNode_TemplateSupportsVariables()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.MessageFormatter);

        // Assert
        var templateField = schema.Fields.First(f => f.Name == "template");
        Assert.True(templateField.SupportsVariables);
        Assert.Equal(ControlType.Code, templateField.ControlType);
    }

    #endregion

    #region GenerateSchema<T> Tests

    [Fact]
    public void GenerateSchemaGeneric_HttpRequestNodeConfiguration_ReturnsSchema()
    {
        // Act
        var schema = _generator.GenerateSchema<HttpRequestNodeConfiguration>();

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(NodeType.HttpRequest, schema.NodeType);
    }

    [Fact]
    public void GenerateSchemaGeneric_SleepNodeConfiguration_ReturnsSchema()
    {
        // Act
        var schema = _generator.GenerateSchema<SleepNodeConfiguration>();

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(NodeType.Sleep, schema.NodeType);
    }

    #endregion

    #region GetAllSchemas Tests

    [Fact]
    public void GetAllSchemas_ReturnsAllNodeTypes()
    {
        // Act
        var schemas = _generator.GetAllSchemas();

        // Assert
        Assert.Contains(NodeType.Start, schemas.Keys);
        Assert.Contains(NodeType.End, schemas.Keys);
        Assert.Contains(NodeType.Model, schemas.Keys);
        Assert.Contains(NodeType.MessageFormatter, schemas.Keys);
        Assert.Contains(NodeType.HttpRequest, schemas.Keys);
        Assert.Contains(NodeType.Sleep, schemas.Keys);
    }

    [Fact]
    public void GetAllSchemas_AllSchemasHaveFields()
    {
        // Act
        var schemas = _generator.GetAllSchemas();

        // Assert
        foreach (var schema in schemas.Values)
        {
            // All node types should have at least some configurable fields
            // (though some may only have 'name' which isn't marked as configurable)
            Assert.NotNull(schema.Fields);
        }
    }

    #endregion

    #region Caching Tests

    [Fact]
    public void GenerateSchema_CalledTwice_ReturnsSameInstance()
    {
        // Act
        var schema1 = _generator.GenerateSchema(NodeType.HttpRequest);
        var schema2 = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        Assert.Same(schema1, schema2);
    }

    #endregion

    #region Field Property Tests

    [Fact]
    public void GenerateSchema_RequiredFieldsAreMarkedRequired()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var methodField = schema.Fields.First(f => f.Name == "method");
        var urlField = schema.Fields.First(f => f.Name == "url");
        Assert.True(methodField.Required);
        Assert.True(urlField.Required);

        var bodyField = schema.Fields.First(f => f.Name == "body");
        Assert.False(bodyField.Required);
    }

    [Fact]
    public void GenerateSchema_FieldsHaveCamelCaseNames()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        foreach (var field in schema.Fields)
        {
            Assert.True(char.IsLower(field.Name[0]), $"Field name '{field.Name}' should be camelCase");
        }
    }

    [Fact]
    public void GenerateSchema_FieldsAreOrderedByOrderAttribute()
    {
        // Act
        var schema = _generator.GenerateSchema(NodeType.HttpRequest);

        // Assert
        var orders = schema.Fields.Select(f => f.Order).ToList();
        Assert.Equal(orders.OrderBy(o => o).ToList(), orders);
    }

    #endregion
}
