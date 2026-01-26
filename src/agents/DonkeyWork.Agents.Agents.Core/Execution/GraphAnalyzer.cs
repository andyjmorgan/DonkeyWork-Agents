using System.Text.Json;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Analyzes ReactFlow graph data and determines execution order.
/// </summary>
public class GraphAnalyzer
{
    /// <summary>
    /// Result of graph analysis.
    /// </summary>
    public class GraphAnalysisResult
    {
        /// <summary>
        /// Whether the analysis succeeded.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if analysis failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Execution order (topologically sorted list of node IDs).
        /// </summary>
        public List<string> ExecutionOrder { get; set; } = new();

        /// <summary>
        /// Adjacency list representation (node ID -> list of target node IDs).
        /// </summary>
        public Dictionary<string, List<string>> AdjacencyList { get; set; } = new();

        /// <summary>
        /// Reverse adjacency list (node ID -> list of source node IDs).
        /// </summary>
        public Dictionary<string, List<string>> ReverseAdjacencyList { get; set; } = new();
    }

    /// <summary>
    /// Analyzes the ReactFlow graph and computes execution order.
    /// </summary>
    /// <param name="reactFlowData">The ReactFlow data (nodes and edges).</param>
    /// <returns>Analysis result with execution order or error message.</returns>
    public GraphAnalysisResult Analyze(JsonElement reactFlowData)
    {
        try
        {
            // Extract nodes and edges from ReactFlow data
            if (!reactFlowData.TryGetProperty("nodes", out var nodesElement))
            {
                return new GraphAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = "ReactFlow data missing 'nodes' property"
                };
            }

            if (!reactFlowData.TryGetProperty("edges", out var edgesElement))
            {
                return new GraphAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = "ReactFlow data missing 'edges' property"
                };
            }

            // Parse nodes
            var nodeIds = new HashSet<string>();
            string? startNodeId = null;

            foreach (var node in nodesElement.EnumerateArray())
            {
                if (!node.TryGetProperty("id", out var idElement))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Node missing 'id' property"
                    };
                }

                var nodeId = idElement.GetString();
                if (string.IsNullOrEmpty(nodeId))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Node has null or empty 'id'"
                    };
                }

                nodeIds.Add(nodeId);

                // Find start node
                if (node.TryGetProperty("type", out var typeElement))
                {
                    var nodeType = typeElement.GetString();
                    if (nodeType == "start")
                    {
                        if (startNodeId != null)
                        {
                            return new GraphAnalysisResult
                            {
                                IsValid = false,
                                ErrorMessage = "Multiple start nodes found"
                            };
                        }
                        startNodeId = nodeId;
                    }
                }
            }

            if (startNodeId == null)
            {
                return new GraphAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = "No start node found"
                };
            }

            // Build adjacency lists
            var adjacencyList = new Dictionary<string, List<string>>();
            var reverseAdjacencyList = new Dictionary<string, List<string>>();

            foreach (var nodeId in nodeIds)
            {
                adjacencyList[nodeId] = new List<string>();
                reverseAdjacencyList[nodeId] = new List<string>();
            }

            foreach (var edge in edgesElement.EnumerateArray())
            {
                if (!edge.TryGetProperty("source", out var sourceElement) ||
                    !edge.TryGetProperty("target", out var targetElement))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Edge missing 'source' or 'target' property"
                    };
                }

                var source = sourceElement.GetString();
                var target = targetElement.GetString();

                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Edge has null or empty source or target"
                    };
                }

                if (!nodeIds.Contains(source) || !nodeIds.Contains(target))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Edge references non-existent node: {source} -> {target}"
                    };
                }

                adjacencyList[source].Add(target);
                reverseAdjacencyList[target].Add(source);
            }

            // Check connectivity: all nodes must be reachable from start
            var reachableNodes = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(startNodeId);
            reachableNodes.Add(startNodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in adjacencyList[current])
                {
                    if (reachableNodes.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (reachableNodes.Count != nodeIds.Count)
            {
                var unreachable = nodeIds.Except(reachableNodes).ToList();
                return new GraphAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = $"Unreachable nodes from start: {string.Join(", ", unreachable)}"
                };
            }

            // Topological sort using Kahn's algorithm
            var inDegree = nodeIds.ToDictionary(id => id, id => reverseAdjacencyList[id].Count);
            var sortQueue = new Queue<string>();
            var executionOrder = new List<string>();

            // Start with nodes that have no incoming edges (should just be start node)
            foreach (var nodeId in nodeIds)
            {
                if (inDegree[nodeId] == 0)
                {
                    sortQueue.Enqueue(nodeId);
                }
            }

            while (sortQueue.Count > 0)
            {
                var current = sortQueue.Dequeue();
                executionOrder.Add(current);

                foreach (var neighbor in adjacencyList[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        sortQueue.Enqueue(neighbor);
                    }
                }
            }

            // Check for cycles
            if (executionOrder.Count != nodeIds.Count)
            {
                return new GraphAnalysisResult
                {
                    IsValid = false,
                    ErrorMessage = "Graph contains cycles"
                };
            }

            return new GraphAnalysisResult
            {
                IsValid = true,
                ExecutionOrder = executionOrder,
                AdjacencyList = adjacencyList,
                ReverseAdjacencyList = reverseAdjacencyList
            };
        }
        catch (Exception ex)
        {
            return new GraphAnalysisResult
            {
                IsValid = false,
                ErrorMessage = $"Graph analysis failed: {ex.Message}"
            };
        }
    }
}
