using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Persistence.Entities.Agents;

namespace DonkeyWork.Agents.Agents.Core.Tests.Helpers;

/// <summary>
/// Builder class for creating test data.
/// Provides fluent API for building complex test objects.
/// </summary>
public class TestDataBuilder
{
    private readonly Guid _defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Creates a default input schema JSON string for tests.
    /// </summary>
    public static string CreateInputSchemaJson() => """{"type":"object","properties":{"input":{"type":"string"}},"required":["input"]}""";

    /// <summary>
    /// Creates a default input schema as JsonDocument for tests.
    /// </summary>
    public static JsonDocument CreateInputSchemaDocument() => JsonDocument.Parse(CreateInputSchemaJson());

    /// <summary>
    /// Creates a ReactFlowNode with the specified parameters.
    /// </summary>
    public static ReactFlowNode CreateReactFlowNode(Guid id, NodeType nodeType, string label, double x = 100, double y = 100)
    {
        return new ReactFlowNode
        {
            Id = id,
            Type = "schemaNode",
            Position = new ReactFlowPosition { X = x, Y = y },
            Data = new ReactFlowNodeData
            {
                NodeType = nodeType,
                Label = label,
                DisplayName = label
            }
        };
    }

    /// <summary>
    /// Creates a ReactFlowEdge with the specified parameters.
    /// </summary>
    public static ReactFlowEdge CreateReactFlowEdge(Guid source, Guid target, Guid? id = null)
    {
        return new ReactFlowEdge
        {
            Id = id ?? Guid.NewGuid(),
            Source = source,
            Target = target
        };
    }

    /// <summary>
    /// Creates a basic CreateAgentRequestV1 with default values.
    /// </summary>
    public static CreateAgentRequestV1 CreateAgentRequest(string name = "test-agent", string? description = "Test description")
    {
        return new CreateAgentRequestV1
        {
            Name = name,
            Description = description
        };
    }

    /// <summary>
    /// Creates a basic SaveAgentVersionRequestV1 with Start -> End flow.
    /// Alias for CreateSaveVersionRequest for backwards compatibility.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateBasicSaveVersionRequest()
    {
        return CreateSaveVersionRequest();
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with Start -> End flow.
    /// Uses the new Contracts.Nodes configuration format.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequest()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 250)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object", properties = new { input = new { type = "string" } }, required = new[] { "input" } } },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with credential mappings.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithCredentials(List<CredentialMappingV1> credentials)
    {
        var request = CreateSaveVersionRequest();
        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = request.ReactFlowData,
            NodeConfigurations = request.NodeConfigurations,
            InputSchema = request.InputSchema,
            OutputSchema = request.OutputSchema,
            CredentialMappings = credentials
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with duplicate node names.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithDuplicateNames()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "duplicate_name", 100, 100),
                CreateReactFlowNode(endNodeId, NodeType.End, "duplicate_name", 100, 250)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "duplicate_name", inputSchema = new { type = "object" } },
            [endNodeId] = new { type = "End", name = "duplicate_name" }  // Duplicate name
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with missing start node.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithoutStartNode()
    {
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 250)
            ],
            Edges = [],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with a cyclic graph (Start -> Model -> Start).
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithCycle()
    {
        var startNodeId = Guid.NewGuid();
        var modelNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(modelNodeId, NodeType.Model, "model_1", 100, 200),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 300)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, modelNodeId),
                CreateReactFlowEdge(modelNodeId, startNodeId),  // Cycle back to start
                CreateReactFlowEdge(modelNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } },
            [modelNodeId] = new { type = "Model", name = "model_1" },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates an AgentEntity with default test values.
    /// </summary>
    public AgentEntity CreateAgentEntity(Guid? id = null, Guid? userId = null, string name = "test-agent")
    {
        return new AgentEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            Name = name,
            Description = "Test description",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an AgentVersionEntity with default test values.
    /// Uses typed ReactFlowData and Dictionary for NodeConfigurations.
    /// </summary>
    public AgentVersionEntity CreateAgentVersionEntity(
        Guid? id = null,
        Guid? agentId = null,
        Guid? userId = null,
        int versionNumber = 1,
        bool isDraft = true)
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 250)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, NodeConfiguration>
        {
            [startNodeId] = new StartNodeConfiguration { Name = "start_1", InputSchema = JsonSerializer.SerializeToElement(new { type = "object", properties = new { input = new { type = "string" } }, required = new[] { "input" } }) },
            [endNodeId] = new EndNodeConfiguration { Name = "end_1" }
        };

        return new AgentVersionEntity
        {
            Id = id ?? Guid.NewGuid(),
            AgentId = agentId ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            ReactFlowData = reactFlowData,
            NodeConfigurations = nodeConfigurations,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            PublishedAt = isDraft ? null : DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with missing end node.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithoutEndNode()
    {
        var startNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100)
            ],
            Edges = [],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with disconnected nodes (Start and End not connected).
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithDisconnectedNodes()
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var modelNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(modelNodeId, NodeType.Model, "model_1", 100, 200),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 300)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, modelNodeId)
                // Note: No edge from model to end - end is disconnected
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } },
            [modelNodeId] = new { type = "Model", name = "model_1" },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with multiple start nodes.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithMultipleStartNodes()
    {
        var startNodeId1 = Guid.NewGuid();
        var startNodeId2 = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId1, NodeType.Start, "start_1", 50, 100),
                CreateReactFlowNode(startNodeId2, NodeType.Start, "start_2", 150, 100),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 250)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId1, endNodeId),
                CreateReactFlowEdge(startNodeId2, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId1] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } },
            [startNodeId2] = new { type = "Start", name = "start_2", inputSchema = new { type = "object" } },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a credential mapping for testing.
    /// </summary>
    public static CredentialMappingV1 CreateCredentialMapping(Guid? nodeId = null, Guid? credentialId = null)
    {
        return new CredentialMappingV1
        {
            NodeId = nodeId ?? Guid.NewGuid(),
            CredentialId = credentialId ?? Guid.NewGuid()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with an HttpRequest node (Start -> HttpRequest -> End).
    /// Uses the new Contracts.Nodes configuration format with dedicated node types.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithHttpRequestNode()
    {
        var startNodeId = Guid.NewGuid();
        var httpRequestNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(httpRequestNodeId, NodeType.HttpRequest, "http_request_1", 100, 200),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 300)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, httpRequestNodeId),
                CreateReactFlowEdge(httpRequestNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } },
            [httpRequestNodeId] = new
            {
                type = "HttpRequest",
                name = "http_request_1",
                method = "GET",
                url = "https://example.com",
                timeoutSeconds = 30
            },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with an HttpRequest node missing required properties.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithInvalidHttpRequestNode()
    {
        var startNodeId = Guid.NewGuid();
        var httpRequestNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                CreateReactFlowNode(startNodeId, NodeType.Start, "start_1", 100, 100),
                CreateReactFlowNode(httpRequestNodeId, NodeType.HttpRequest, "http_request_1", 100, 200),
                CreateReactFlowNode(endNodeId, NodeType.End, "end_1", 100, 300)
            ],
            Edges =
            [
                CreateReactFlowEdge(startNodeId, httpRequestNodeId),
                CreateReactFlowEdge(httpRequestNodeId, endNodeId)
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, object>
        {
            [startNodeId] = new { type = "Start", name = "start_1", inputSchema = new { type = "object" } },
            [httpRequestNodeId] = new
            {
                type = "HttpRequest",
                name = "http_request_1"
                // Missing required properties: method and url
            },
            [endNodeId] = new { type = "End", name = "end_1" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonSerializer.SerializeToElement(nodeConfigurations),
            InputSchema = CreateInputSchemaDocument(),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }
}
