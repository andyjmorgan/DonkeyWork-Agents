using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Core.Execution;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution;

/// <summary>
/// Unit tests for GraphAnalyzer.
/// Tests all validation rules: DAG validation, connectivity, single start/end nodes, and edge validation.
/// </summary>
public class GraphAnalyzerTests
{
    private readonly GraphAnalyzer _analyzer = new();

    // Predefined GUIDs for consistent testing
    private static readonly Guid StartNodeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EndNodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ModelNode1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ModelNode2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ModelNode3Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SecondStartNodeId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid NonExistentNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    #region Valid Graph Tests

    [Fact]
    public void Analyze_WithValidSimpleGraph_ReturnsSuccess()
    {
        // Arrange - Start -> End
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ExecutionOrder.Count);
        Assert.Equal(StartNodeId, result.ExecutionOrder[0]);
        Assert.Equal(EndNodeId, result.ExecutionOrder[1]);
    }

    [Fact]
    public void Analyze_WithThreeNodeLinearGraph_ReturnsCorrectOrder()
    {
        // Arrange - Start -> Model -> End
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.ExecutionOrder.Count);
        Assert.Equal(StartNodeId, result.ExecutionOrder[0]);
        Assert.Equal(ModelNode1Id, result.ExecutionOrder[1]);
        Assert.Equal(EndNodeId, result.ExecutionOrder[2]);
    }

    [Fact]
    public void Analyze_WithBranchingGraph_ReturnsValidOrder()
    {
        // Arrange - Start -> Model1 -> End
        //                 \-> Model2 -/
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(StartNodeId, ModelNode2Id),
                CreateEdge(ModelNode1Id, EndNodeId),
                CreateEdge(ModelNode2Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(4, result.ExecutionOrder.Count);
        Assert.Equal(StartNodeId, result.ExecutionOrder[0]);
        Assert.Contains(ModelNode1Id, result.ExecutionOrder);
        Assert.Contains(ModelNode2Id, result.ExecutionOrder);
        Assert.Equal(EndNodeId, result.ExecutionOrder[3]);
    }

    [Fact]
    public void Analyze_WithComplexDAG_ReturnsValidOrder()
    {
        // Arrange - Diamond pattern: Start -> A -> C -> End
        //                                  \-> B -/
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model), // A
                CreateNode(ModelNode2Id, NodeType.Model), // B
                CreateNode(ModelNode3Id, NodeType.Model), // C
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(StartNodeId, ModelNode2Id),
                CreateEdge(ModelNode1Id, ModelNode3Id),
                CreateEdge(ModelNode2Id, ModelNode3Id),
                CreateEdge(ModelNode3Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(5, result.ExecutionOrder.Count);
        Assert.Equal(StartNodeId, result.ExecutionOrder[0]);
        Assert.Equal(EndNodeId, result.ExecutionOrder[4]);

        // node-c should come after both node-a and node-b
        var indexA = result.ExecutionOrder.IndexOf(ModelNode1Id);
        var indexB = result.ExecutionOrder.IndexOf(ModelNode2Id);
        var indexC = result.ExecutionOrder.IndexOf(ModelNode3Id);
        Assert.True(indexC > indexA);
        Assert.True(indexC > indexB);
    }

    #endregion

    #region Missing Start Node Tests

    [Fact]
    public void Analyze_WithoutStartNode_ReturnsError()
    {
        // Arrange - Only End node, no Start
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: Array.Empty<ReactFlowEdge>()
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("No start node found", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithOnlyModelNodes_ReturnsError()
    {
        // Arrange - No start node
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model)
            },
            edges: new[]
            {
                CreateEdge(ModelNode1Id, ModelNode2Id)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("No start node found", result.ErrorMessage);
    }

    #endregion

    #region Multiple Start Nodes Tests

    [Fact]
    public void Analyze_WithMultipleStartNodes_ReturnsError()
    {
        // Arrange - Two start nodes
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(SecondStartNodeId, NodeType.Start),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, EndNodeId),
                CreateEdge(SecondStartNodeId, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Multiple start nodes found", result.ErrorMessage);
    }

    #endregion

    #region Cycle Detection Tests

    [Fact]
    public void Analyze_WithSimpleCycle_ReturnsError()
    {
        // Arrange - Start -> Model -> Model (back to itself)
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, ModelNode1Id), // Self-loop
                CreateEdge(ModelNode1Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Graph contains cycles", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithTwoNodeCycle_ReturnsError()
    {
        // Arrange - Start -> Model1 <-> Model2 -> End
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, ModelNode2Id),
                CreateEdge(ModelNode2Id, ModelNode1Id), // Cycle
                CreateEdge(ModelNode2Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Graph contains cycles", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithComplexCycle_ReturnsError()
    {
        // Arrange - Start -> A -> B -> C -> A (cycle back to A)
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model),
                CreateNode(ModelNode3Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, ModelNode2Id),
                CreateEdge(ModelNode2Id, ModelNode3Id),
                CreateEdge(ModelNode3Id, ModelNode1Id), // Cycle
                CreateEdge(ModelNode3Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Graph contains cycles", result.ErrorMessage);
    }

    #endregion

    #region Disconnected Nodes Tests

    [Fact]
    public void Analyze_WithDisconnectedNode_ReturnsError()
    {
        // Arrange - Start -> Model1, Model2 (disconnected), End (disconnected)
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model), // Disconnected
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id)
                // model-2 and end-1 have no edges
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unreachable nodes from start", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithDisconnectedEndNode_ReturnsError()
    {
        // Arrange - Start -> Model, but End is disconnected
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id)
                // End node is not connected
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unreachable nodes from start", result.ErrorMessage);
        Assert.Contains(EndNodeId.ToString(), result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithMultipleDisconnectedNodes_ReturnsError()
    {
        // Arrange - Start -> End, but Model1 and Model2 are disconnected
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(ModelNode2Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, EndNodeId)
                // model-1 and model-2 are disconnected
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unreachable nodes from start", result.ErrorMessage);
    }

    #endregion

    #region Invalid Edge Tests

    [Fact]
    public void Analyze_WithEdgeToNonExistentNode_ReturnsError()
    {
        // Arrange - Edge references node that doesn't exist
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, NonExistentNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Edge references non-existent node", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithEdgeFromNonExistentNode_ReturnsError()
    {
        // Arrange - Edge source doesn't exist
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(NonExistentNodeId, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Edge references non-existent node", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithEmptyGuidNodeId_ReturnsError()
    {
        // Arrange - Node with empty GUID
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(Guid.Empty, NodeType.Start)
            },
            edges: Array.Empty<ReactFlowEdge>()
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Node has empty", result.ErrorMessage);
    }

    #endregion

    #region Empty Graph Tests

    [Fact]
    public void Analyze_WithEmptyGraph_ReturnsError()
    {
        // Arrange
        var graphData = new ReactFlowData
        {
            Nodes = new List<ReactFlowNode>(),
            Edges = new List<ReactFlowEdge>()
        };

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("No start node found", result.ErrorMessage);
    }

    #endregion

    #region Adjacency List Tests

    [Fact]
    public void Analyze_BuildsCorrectAdjacencyList()
    {
        // Arrange
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.AdjacencyList);
        Assert.Contains(ModelNode1Id, result.AdjacencyList[StartNodeId]);
        Assert.Contains(EndNodeId, result.AdjacencyList[ModelNode1Id]);
        Assert.Empty(result.AdjacencyList[EndNodeId]);
    }

    [Fact]
    public void Analyze_BuildsCorrectReverseAdjacencyList()
    {
        // Arrange
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode(StartNodeId, NodeType.Start),
                CreateNode(ModelNode1Id, NodeType.Model),
                CreateNode(EndNodeId, NodeType.End)
            },
            edges: new[]
            {
                CreateEdge(StartNodeId, ModelNode1Id),
                CreateEdge(ModelNode1Id, EndNodeId)
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.ReverseAdjacencyList);
        Assert.Empty(result.ReverseAdjacencyList[StartNodeId]);
        Assert.Contains(StartNodeId, result.ReverseAdjacencyList[ModelNode1Id]);
        Assert.Contains(ModelNode1Id, result.ReverseAdjacencyList[EndNodeId]);
    }

    #endregion

    #region Helper Methods

    private static ReactFlowData CreateGraphData(ReactFlowNode[] nodes, ReactFlowEdge[] edges)
    {
        return new ReactFlowData
        {
            Nodes = nodes.ToList(),
            Edges = edges.ToList(),
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };
    }

    private static ReactFlowNode CreateNode(Guid id, NodeType nodeType)
    {
        return new ReactFlowNode
        {
            Id = id,
            Type = GetReactFlowType(nodeType),
            Position = new ReactFlowPosition { X = 100, Y = 100 },
            Data = new ReactFlowNodeData
            {
                NodeType = nodeType,
                Label = id.ToString(),
                DisplayName = $"{nodeType} Node"
            }
        };
    }

    private static ReactFlowEdge CreateEdge(Guid source, Guid target)
    {
        return new ReactFlowEdge
        {
            Id = Guid.NewGuid(),
            Source = source,
            Target = target
        };
    }

    private static string GetReactFlowType(NodeType nodeType) => nodeType switch
    {
        NodeType.Start => "start",
        NodeType.End => "end",
        NodeType.Model => "model",
        NodeType.MessageFormatter => "messageFormatter",
        NodeType.HttpRequest => "httpRequest",
        NodeType.Sleep => "sleep",
        _ => "unknown"
    };

    #endregion
}
