using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
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
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequest()
    {
        var startNodeId = "start-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 250 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new
            {
                input = new { type = "string" }
            },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
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
        var startNodeId = "start-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 250 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "duplicate_name" },
            [endNodeId] = new { type = "end", name = "duplicate_name" }  // Duplicate name
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with missing start node.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithoutStartNode()
    {
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = endNodeId, type = "end", position = new { x = 100, y = 250 }, data = new { label = "end" } }
            },
            edges = Array.Empty<object>(),
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with a cyclic graph (Start -> Model -> Start).
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithCycle()
    {
        var startNodeId = "start-1";
        var modelNodeId = "model-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = modelNodeId, type = "model", position = new { x = 100, y = 200 }, data = new { label = "model" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 300 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = modelNodeId },
                new { id = "e2", source = modelNodeId, target = startNodeId },  // Cycle back to start
                new { id = "e3", source = modelNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" },
            [modelNodeId] = new { type = "model", name = "model_1" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
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
    /// </summary>
    public AgentVersionEntity CreateAgentVersionEntity(
        Guid? id = null,
        Guid? agentId = null,
        Guid? userId = null,
        int versionNumber = 1,
        bool isDraft = true)
    {
        var startNodeId = "start-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 250 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new AgentVersionEntity
        {
            Id = id ?? Guid.NewGuid(),
            AgentId = agentId ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            InputSchema = JsonSerializer.Serialize(inputSchema),
            OutputSchema = null,
            ReactFlowData = JsonSerializer.Serialize(reactFlowData),
            NodeConfigurations = JsonSerializer.Serialize(nodeConfigurations),
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
        var startNodeId = "start-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } }
            },
            edges = Array.Empty<object>(),
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with disconnected nodes (Start and End not connected).
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithDisconnectedNodes()
    {
        var startNodeId = "start-1";
        var endNodeId = "end-1";
        var modelNodeId = "model-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = modelNodeId, type = "model", position = new { x = 100, y = 200 }, data = new { label = "model" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 300 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = modelNodeId }
                // Note: No edge from model to end - end is disconnected
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" },
            [modelNodeId] = new { type = "model", name = "model_1" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with multiple start nodes.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithMultipleStartNodes()
    {
        var startNodeId1 = "start-1";
        var startNodeId2 = "start-2";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId1, type = "start", position = new { x = 50, y = 100 }, data = new { label = "start 1" } },
                new { id = startNodeId2, type = "start", position = new { x = 150, y = 100 }, data = new { label = "start 2" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 250 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId1, target = endNodeId },
                new { id = "e2", source = startNodeId2, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId1] = new { type = "start", name = "start_1" },
            [startNodeId2] = new { type = "start", name = "start_2" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a credential mapping for testing.
    /// </summary>
    public static CredentialMappingV1 CreateCredentialMapping(string nodeId = "model-1", Guid? credentialId = null)
    {
        return new CredentialMappingV1
        {
            NodeId = nodeId,
            CredentialId = credentialId ?? Guid.NewGuid()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with an action node (Start -> Action -> End).
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithActionNode(string actionType = "http_request")
    {
        var startNodeId = "start-1";
        var actionNodeId = "action-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = actionNodeId, type = "action", position = new { x = 100, y = 200 }, data = new { label = "action" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 300 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = actionNodeId },
                new { id = "e2", source = actionNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { name = "start_1" },
            [actionNodeId] = new
            {
                name = "action_1",
                actionType = actionType,
                displayName = "Test HTTP Request",
                parameters = new { url = "https://example.com", method = "GET" }
            },
            [endNodeId] = new { name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with an action node that has missing actionType.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithInvalidActionNode()
    {
        var startNodeId = "start-1";
        var actionNodeId = "action-1";
        var endNodeId = "end-1";

        var reactFlowData = new
        {
            nodes = new[]
            {
                new { id = startNodeId, type = "start", position = new { x = 100, y = 100 }, data = new { label = "start" } },
                new { id = actionNodeId, type = "action", position = new { x = 100, y = 200 }, data = new { label = "action" } },
                new { id = endNodeId, type = "end", position = new { x = 100, y = 300 }, data = new { label = "end" } }
            },
            edges = new[]
            {
                new { id = "e1", source = startNodeId, target = actionNodeId },
                new { id = "e2", source = actionNodeId, target = endNodeId }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { name = "start_1" },
            [actionNodeId] = new
            {
                name = "action_1",
                // Missing actionType
                parameters = new { url = "https://example.com" }
            },
            [endNodeId] = new { name = "end_1" }
        };

        var inputSchema = new
        {
            type = "object",
            properties = new { input = new { type = "string" } },
            required = new[] { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(reactFlowData)),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(nodeConfigurations)),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(inputSchema)),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }
}
