using System.Text.Json;
using DonkeyWork.Agents.Agents.Core.Execution;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution;

/// <summary>
/// Unit tests for GraphAnalyzer.
/// Tests all validation rules: DAG validation, connectivity, single start/end nodes, and edge validation.
/// </summary>
public class GraphAnalyzerTests
{
    private readonly GraphAnalyzer _analyzer = new();

    #region Valid Graph Tests

    [Fact]
    public void Analyze_WithValidSimpleGraph_ReturnsSuccess()
    {
        // Arrange - Start -> End
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.ExecutionOrder.Count);
        Assert.Equal("start-1", result.ExecutionOrder[0]);
        Assert.Equal("end-1", result.ExecutionOrder[1]);
    }

    [Fact]
    public void Analyze_WithThreeNodeLinearGraph_ReturnsCorrectOrder()
    {
        // Arrange - Start -> Model -> End
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("model-1", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, result.ExecutionOrder.Count);
        Assert.Equal("start-1", result.ExecutionOrder[0]);
        Assert.Equal("model-1", result.ExecutionOrder[1]);
        Assert.Equal("end-1", result.ExecutionOrder[2]);
    }

    [Fact]
    public void Analyze_WithBranchingGraph_ReturnsValidOrder()
    {
        // Arrange - Start -> Model1 -> End
        //                 \-> Model2 -/
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("model-2", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("start-1", "model-2"),
                CreateEdge("model-1", "end-1"),
                CreateEdge("model-2", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(4, result.ExecutionOrder.Count);
        Assert.Equal("start-1", result.ExecutionOrder[0]);
        Assert.Contains("model-1", result.ExecutionOrder);
        Assert.Contains("model-2", result.ExecutionOrder);
        Assert.Equal("end-1", result.ExecutionOrder[3]);
    }

    [Fact]
    public void Analyze_WithComplexDAG_ReturnsValidOrder()
    {
        // Arrange - Diamond pattern: Start -> A -> C -> End
        //                                  \-> B -/
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("node-a", "model"),
                CreateNode("node-b", "model"),
                CreateNode("node-c", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "node-a"),
                CreateEdge("start-1", "node-b"),
                CreateEdge("node-a", "node-c"),
                CreateEdge("node-b", "node-c"),
                CreateEdge("node-c", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(5, result.ExecutionOrder.Count);
        Assert.Equal("start-1", result.ExecutionOrder[0]);
        Assert.Equal("end-1", result.ExecutionOrder[4]);

        // node-c should come after both node-a and node-b
        var indexA = result.ExecutionOrder.IndexOf("node-a");
        var indexB = result.ExecutionOrder.IndexOf("node-b");
        var indexC = result.ExecutionOrder.IndexOf("node-c");
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
                CreateNode("end-1", "end")
            },
            edges: Array.Empty<object>()
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
                CreateNode("model-1", "model"),
                CreateNode("model-2", "model")
            },
            edges: new[]
            {
                CreateEdge("model-1", "model-2")
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
                CreateNode("start-1", "start"),
                CreateNode("start-2", "start"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "end-1"),
                CreateEdge("start-2", "end-1")
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
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("model-1", "model-1"), // Self-loop
                CreateEdge("model-1", "end-1")
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
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("model-2", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("model-1", "model-2"),
                CreateEdge("model-2", "model-1"), // Cycle
                CreateEdge("model-2", "end-1")
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
                CreateNode("start-1", "start"),
                CreateNode("node-a", "model"),
                CreateNode("node-b", "model"),
                CreateNode("node-c", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "node-a"),
                CreateEdge("node-a", "node-b"),
                CreateEdge("node-b", "node-c"),
                CreateEdge("node-c", "node-a"), // Cycle
                CreateEdge("node-c", "end-1")
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
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("model-2", "model"), // Disconnected
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1")
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
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1")
                // End node is not connected
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unreachable nodes from start", result.ErrorMessage);
        Assert.Contains("end-1", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithMultipleDisconnectedNodes_ReturnsError()
    {
        // Arrange - Start -> End, but Model1 and Model2 are disconnected
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("model-2", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "end-1")
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
                CreateNode("start-1", "start"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "non-existent-node")
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
                CreateNode("start-1", "start"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("non-existent-node", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Edge references non-existent node", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithNullEdgeSource_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""id"": ""start-1"", ""type"": ""start""},
                {""id"": ""end-1"", ""type"": ""end""}
            ],
            ""edges"": [
                {""source"": null, ""target"": ""end-1""}
            ]
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("null or empty source or target", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithEmptyEdgeTarget_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""id"": ""start-1"", ""type"": ""start""},
                {""id"": ""end-1"", ""type"": ""end""}
            ],
            ""edges"": [
                {""source"": ""start-1"", ""target"": """"}
            ]
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("null or empty source or target", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithMissingEdgeProperties_ReturnsError()
    {
        // Arrange - Edge missing source property
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""id"": ""start-1"", ""type"": ""start""},
                {""id"": ""end-1"", ""type"": ""end""}
            ],
            ""edges"": [
                {""target"": ""end-1""}
            ]
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Edge missing 'source' or 'target'", result.ErrorMessage);
    }

    #endregion

    #region Invalid Node Tests

    [Fact]
    public void Analyze_WithMissingNodeId_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""type"": ""start""}
            ],
            ""edges"": []
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Node missing 'id' property", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithNullNodeId_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""id"": null, ""type"": ""start""}
            ],
            ""edges"": []
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Node has null or empty 'id'", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithEmptyNodeId_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [
                {""id"": """", ""type"": ""start""}
            ],
            ""edges"": []
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Node has null or empty 'id'", result.ErrorMessage);
    }

    #endregion

    #region Missing Graph Properties Tests

    [Fact]
    public void Analyze_WithMissingNodesProperty_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""edges"": []
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ReactFlow data missing 'nodes' property", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithMissingEdgesProperty_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [{""id"": ""start-1"", ""type"": ""start""}]
        }");

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ReactFlow data missing 'edges' property", result.ErrorMessage);
    }

    [Fact]
    public void Analyze_WithEmptyGraph_ReturnsError()
    {
        // Arrange
        var graphData = JsonSerializer.Deserialize<JsonElement>(@"{
            ""nodes"": [],
            ""edges"": []
        }");

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
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("model-1", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.AdjacencyList);
        Assert.Contains("model-1", result.AdjacencyList["start-1"]);
        Assert.Contains("end-1", result.AdjacencyList["model-1"]);
        Assert.Empty(result.AdjacencyList["end-1"]);
    }

    [Fact]
    public void Analyze_BuildsCorrectReverseAdjacencyList()
    {
        // Arrange
        var graphData = CreateGraphData(
            nodes: new[]
            {
                CreateNode("start-1", "start"),
                CreateNode("model-1", "model"),
                CreateNode("end-1", "end")
            },
            edges: new[]
            {
                CreateEdge("start-1", "model-1"),
                CreateEdge("model-1", "end-1")
            }
        );

        // Act
        var result = _analyzer.Analyze(graphData);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.ReverseAdjacencyList);
        Assert.Empty(result.ReverseAdjacencyList["start-1"]);
        Assert.Contains("start-1", result.ReverseAdjacencyList["model-1"]);
        Assert.Contains("model-1", result.ReverseAdjacencyList["end-1"]);
    }

    #endregion

    #region Helper Methods

    private JsonElement CreateGraphData(object[] nodes, object[] edges)
    {
        var graphObject = new
        {
            nodes = nodes,
            edges = edges,
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(graphObject));
    }

    private object CreateNode(string id, string type)
    {
        return new
        {
            id = id,
            type = type,
            position = new { x = 100, y = 100 },
            data = new { label = id }
        };
    }

    private object CreateEdge(string source, string target)
    {
        return new
        {
            id = $"{source}-{target}",
            source = source,
            target = target
        };
    }

    #endregion
}
