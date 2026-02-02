using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Registry;
using Xunit;

namespace DonkeyWork.Agents.Agents.Core.Tests.Nodes.Registry;

/// <summary>
/// Unit tests for NodeConfigurationRegistry.
/// Tests serialization, deserialization, and type discovery.
/// </summary>
public class NodeConfigurationRegistryTests
{
    private readonly NodeConfigurationRegistry _registry = NodeConfigurationRegistry.Instance;

    #region GetConfigurationType Tests

    [Theory]
    [InlineData(NodeType.Start, typeof(StartNodeConfiguration))]
    [InlineData(NodeType.End, typeof(EndNodeConfiguration))]
    [InlineData(NodeType.Model, typeof(ModelNodeConfiguration))]
    [InlineData(NodeType.MessageFormatter, typeof(MessageFormatterNodeConfiguration))]
    [InlineData(NodeType.HttpRequest, typeof(HttpRequestNodeConfiguration))]
    [InlineData(NodeType.Sleep, typeof(SleepNodeConfiguration))]
    public void GetConfigurationType_WithValidNodeType_ReturnsCorrectType(NodeType nodeType, Type expectedType)
    {
        // Act
        var result = _registry.GetConfigurationType(nodeType);

        // Assert
        Assert.Equal(expectedType, result);
    }

    #endregion

    #region Serialize/Deserialize Tests

    [Fact]
    public void Serialize_StartNodeConfiguration_IncludesTypeDiscriminator()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var config = new StartNodeConfiguration { Name = "start_1", InputSchema = inputSchema };

        // Act
        var json = _registry.Serialize(config);

        // Assert
        Assert.Contains("\"type\":\"Start\"", json);
        Assert.Contains("\"name\":\"start_1\"", json);
    }

    [Fact]
    public void Serialize_EndNodeConfiguration_IncludesTypeDiscriminator()
    {
        // Arrange
        var config = new EndNodeConfiguration { Name = "end_1" };

        // Act
        var json = _registry.Serialize(config);

        // Assert
        Assert.Contains("\"type\":\"End\"", json);
        Assert.Contains("\"name\":\"end_1\"", json);
    }

    [Fact]
    public void Deserialize_StartNodeConfiguration_ReturnsCorrectType()
    {
        // Arrange
        var json = "{\"type\":\"Start\",\"name\":\"start_1\",\"inputSchema\":{\"type\":\"object\"}}";

        // Act
        var result = _registry.Deserialize(json);

        // Assert
        Assert.IsType<StartNodeConfiguration>(result);
        Assert.Equal("start_1", result.Name);
    }

    [Fact]
    public void Deserialize_EndNodeConfiguration_ReturnsCorrectType()
    {
        // Arrange
        var json = "{\"type\":\"End\",\"name\":\"end_1\"}";

        // Act
        var result = _registry.Deserialize(json);

        // Assert
        Assert.IsType<EndNodeConfiguration>(result);
        Assert.Equal("end_1", result.Name);
    }

    [Fact]
    public void Deserialize_HttpRequestNodeConfiguration_ReturnsCorrectType()
    {
        // Arrange
        var json = "{\"type\":\"HttpRequest\",\"name\":\"http_1\",\"method\":\"GET\",\"url\":\"https://example.com\",\"timeoutSeconds\":30}";

        // Act
        var result = _registry.Deserialize(json);

        // Assert
        Assert.IsType<HttpRequestNodeConfiguration>(result);
        var httpConfig = (HttpRequestNodeConfiguration)result;
        Assert.Equal("http_1", httpConfig.Name);
        Assert.Equal(Contracts.Nodes.Enums.HttpMethod.Get, httpConfig.Method);
        Assert.Equal("https://example.com", httpConfig.Url);
    }

    [Fact]
    public void Deserialize_SleepNodeConfiguration_ReturnsCorrectType()
    {
        // Arrange
        var json = "{\"type\":\"Sleep\",\"name\":\"sleep_1\",\"durationSeconds\":1.5}";

        // Act
        var result = _registry.Deserialize(json);

        // Assert
        Assert.IsType<SleepNodeConfiguration>(result);
        var sleepConfig = (SleepNodeConfiguration)result;
        Assert.Equal("sleep_1", sleepConfig.Name);
        Assert.Equal(1.5, sleepConfig.DurationSeconds);
    }

    [Fact]
    public void RoundTrip_AllNodeTypes_PreservesData()
    {
        // Arrange
        var inputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;
        var configs = new NodeConfiguration[]
        {
            new StartNodeConfiguration { Name = "start_1", InputSchema = inputSchema },
            new EndNodeConfiguration { Name = "end_1" },
            new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 0.5 },
            new MessageFormatterNodeConfiguration { Name = "formatter_1", Template = "Hello {{name}}" },
            new HttpRequestNodeConfiguration { Name = "http_1", Method = Contracts.Nodes.Enums.HttpMethod.Post, Url = "https://api.example.com" }
        };

        foreach (var config in configs)
        {
            // Act
            var json = _registry.Serialize(config);
            var result = _registry.Deserialize(json);

            // Assert
            Assert.Equal(config.GetType(), result.GetType());
            Assert.Equal(config.Name, result.Name);
            Assert.Equal(config.NodeType, result.NodeType);
        }
    }

    #endregion

    #region SerializeToElement Tests

    [Fact]
    public void SerializeToElement_ReturnsJsonElement()
    {
        // Arrange
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 0.1 };

        // Act
        var element = _registry.SerializeToElement(config);

        // Assert
        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.True(element.TryGetProperty("type", out var typeProperty));
        Assert.Equal("Sleep", typeProperty.GetString());
    }

    #endregion

    #region DeserializeConfigurations Tests

    [Fact]
    public void DeserializeConfigurations_WithDictionary_ReturnsCorrectTypes()
    {
        // Arrange
        var startNodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var endNodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var json = @"{
            ""11111111-1111-1111-1111-111111111111"": {""type"": ""Start"", ""name"": ""start_1"", ""inputSchema"": {}},
            ""22222222-2222-2222-2222-222222222222"": {""type"": ""End"", ""name"": ""end_1""}
        }";

        // Act
        var result = _registry.DeserializeConfigurations(json);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.IsType<StartNodeConfiguration>(result[startNodeId]);
        Assert.IsType<EndNodeConfiguration>(result[endNodeId]);
    }

    #endregion

    #region ConfigurationTypes Tests

    [Fact]
    public void ConfigurationTypes_ContainsAllNodeTypes()
    {
        // Act
        var types = _registry.ConfigurationTypes;

        // Assert
        Assert.Contains(NodeType.Start, types.Keys);
        Assert.Contains(NodeType.End, types.Keys);
        Assert.Contains(NodeType.Model, types.Keys);
        Assert.Contains(NodeType.MessageFormatter, types.Keys);
        Assert.Contains(NodeType.HttpRequest, types.Keys);
        Assert.Contains(NodeType.Sleep, types.Keys);
    }

    #endregion
}
