using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

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
        public List<Guid> ExecutionOrder { get; set; } = [];

        /// <summary>
        /// Adjacency list representation (node ID -> list of target node IDs).
        /// </summary>
        public Dictionary<Guid, List<Guid>> AdjacencyList { get; set; } = new();

        /// <summary>
        /// Reverse adjacency list (node ID -> list of source node IDs).
        /// </summary>
        public Dictionary<Guid, List<Guid>> ReverseAdjacencyList { get; set; } = new();
    }

    /// <summary>
    /// Analyzes the ReactFlow graph and computes execution order.
    /// </summary>
    /// <param name="reactFlowData">The typed ReactFlow data (nodes and edges).</param>
    /// <returns>Analysis result with execution order or error message.</returns>
    public GraphAnalysisResult Analyze(ReactFlowData reactFlowData)
    {
        try
        {
            var nodes = reactFlowData.Nodes;
            var edges = reactFlowData.Edges;

            // Build node ID set and find start node
            var nodeIds = new HashSet<Guid>();
            Guid? startNodeId = null;

            foreach (var node in nodes)
            {
                if (node.Id == Guid.Empty)
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Node has empty id"
                    };
                }

                nodeIds.Add(node.Id);

                // Find start node using typed enum
                if (node.Data.NodeType == NodeType.Start)
                {
                    if (startNodeId != null)
                    {
                        return new GraphAnalysisResult
                        {
                            IsValid = false,
                            ErrorMessage = "Multiple start nodes found"
                        };
                    }
                    startNodeId = node.Id;
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
            var adjacencyList = nodeIds.ToDictionary(id => id, _ => new List<Guid>());
            var reverseAdjacencyList = nodeIds.ToDictionary(id => id, _ => new List<Guid>());

            foreach (var edge in edges)
            {
                if (edge.Source == Guid.Empty || edge.Target == Guid.Empty)
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = "Edge has empty source or target"
                    };
                }

                if (!nodeIds.Contains(edge.Source) || !nodeIds.Contains(edge.Target))
                {
                    return new GraphAnalysisResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Edge references non-existent node: {edge.Source} -> {edge.Target}"
                    };
                }

                adjacencyList[edge.Source].Add(edge.Target);
                reverseAdjacencyList[edge.Target].Add(edge.Source);
            }

            // Check connectivity: all nodes must be reachable from start
            var reachableNodes = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(startNodeId.Value);
            reachableNodes.Add(startNodeId.Value);

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
            var sortQueue = new Queue<Guid>();
            var executionOrder = new List<Guid>();

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
