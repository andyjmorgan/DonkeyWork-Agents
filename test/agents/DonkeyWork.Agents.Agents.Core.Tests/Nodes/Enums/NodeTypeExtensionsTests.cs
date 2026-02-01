using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using Xunit;

namespace DonkeyWork.Agents.Agents.Core.Tests.Nodes.Enums;

/// <summary>
/// Unit tests for NodeTypeExtensions.
/// Tests ReactFlow string conversions and type discriminator mappings.
/// </summary>
public class NodeTypeExtensionsTests
{
    #region ToNodeType Tests

    [Theory]
    [InlineData("start", NodeType.Start)]
    [InlineData("end", NodeType.End)]
    [InlineData("model", NodeType.Model)]
    [InlineData("messageFormatter", NodeType.MessageFormatter)]
    [InlineData("httpRequest", NodeType.HttpRequest)]
    [InlineData("sleep", NodeType.Sleep)]
    public void ToNodeType_WithValidReactFlowType_ReturnsCorrectNodeType(string reactFlowType, NodeType expected)
    {
        // Act
        var result = reactFlowType.ToNodeType();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("Start")]
    [InlineData("END")]
    [InlineData("")]
    [InlineData("unknown")]
    public void ToNodeType_WithInvalidReactFlowType_ThrowsArgumentException(string reactFlowType)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => reactFlowType.ToNodeType());
        Assert.Contains("Unknown ReactFlow node type", exception.Message);
    }

    #endregion

    #region ToReactFlowType Tests

    [Theory]
    [InlineData(NodeType.Start, "start")]
    [InlineData(NodeType.End, "end")]
    [InlineData(NodeType.Model, "model")]
    [InlineData(NodeType.MessageFormatter, "messageFormatter")]
    [InlineData(NodeType.HttpRequest, "httpRequest")]
    [InlineData(NodeType.Sleep, "sleep")]
    public void ToReactFlowType_WithValidNodeType_ReturnsCorrectString(NodeType nodeType, string expected)
    {
        // Act
        var result = nodeType.ToReactFlowType();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ToTypeDiscriminator Tests

    [Theory]
    [InlineData(NodeType.Start, "Start")]
    [InlineData(NodeType.End, "End")]
    [InlineData(NodeType.Model, "Model")]
    [InlineData(NodeType.MessageFormatter, "MessageFormatter")]
    [InlineData(NodeType.HttpRequest, "HttpRequest")]
    [InlineData(NodeType.Sleep, "Sleep")]
    public void ToTypeDiscriminator_WithValidNodeType_ReturnsCorrectDiscriminator(NodeType nodeType, string expected)
    {
        // Act
        var result = nodeType.ToTypeDiscriminator();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData(NodeType.Start)]
    [InlineData(NodeType.End)]
    [InlineData(NodeType.Model)]
    [InlineData(NodeType.MessageFormatter)]
    [InlineData(NodeType.HttpRequest)]
    [InlineData(NodeType.Sleep)]
    public void RoundTrip_NodeTypeToReactFlowAndBack_ReturnsSameNodeType(NodeType nodeType)
    {
        // Act
        var reactFlowType = nodeType.ToReactFlowType();
        var result = reactFlowType.ToNodeType();

        // Assert
        Assert.Equal(nodeType, result);
    }

    #endregion
}
