using System.Text.Json;
using System.Text.Json.Nodes;
using DonkeyWork.Agents.Agents.Contracts.Models;

namespace DonkeyWork.Agents.Agents.Api.Tests.Helpers;

/// <summary>
/// Helper class for building test data.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a basic CreateAgentRequestV1 with default values.
    /// </summary>
    public static CreateAgentRequestV1 CreateAgentRequest(string name = "test-agent", string? description = "Test agent description")
    {
        return new CreateAgentRequestV1
        {
            Name = name,
            Description = description
        };
    }

    /// <summary>
    /// Creates a basic SaveAgentVersionRequestV1 with Start -> End template.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequest()
    {
        var startNodeId = Guid.NewGuid().ToString();
        var endNodeId = Guid.NewGuid().ToString();

        var reactFlowData = new JsonObject
        {
            ["nodes"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = startNodeId,
                    ["type"] = "start",
                    ["position"] = new JsonObject { ["x"] = 100, ["y"] = 100 },
                    ["data"] = new JsonObject { ["label"] = "start" }
                },
                new JsonObject
                {
                    ["id"] = endNodeId,
                    ["type"] = "end",
                    ["position"] = new JsonObject { ["x"] = 100, ["y"] = 250 },
                    ["data"] = new JsonObject { ["label"] = "end" }
                }
            },
            ["edges"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["source"] = startNodeId,
                    ["target"] = endNodeId
                }
            },
            ["viewport"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["zoom"] = 1 }
        };

        var nodeConfigurations = new JsonObject
        {
            [startNodeId] = new JsonObject
            {
                ["type"] = "start",
                ["name"] = "start_1"
            },
            [endNodeId] = new JsonObject
            {
                ["type"] = "end",
                ["name"] = "end_1"
            }
        };

        var inputSchema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["input"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(reactFlowData.ToJsonString()),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(nodeConfigurations.ToJsonString()),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(inputSchema.ToJsonString()),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>()
        };
    }

    /// <summary>
    /// Creates a SaveAgentVersionRequestV1 with a Model node.
    /// </summary>
    public static SaveAgentVersionRequestV1 CreateSaveVersionRequestWithModel(Guid credentialId)
    {
        var startNodeId = Guid.NewGuid().ToString();
        var modelNodeId = Guid.NewGuid().ToString();
        var endNodeId = Guid.NewGuid().ToString();

        var reactFlowData = new JsonObject
        {
            ["nodes"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = startNodeId,
                    ["type"] = "start",
                    ["position"] = new JsonObject { ["x"] = 100, ["y"] = 100 },
                    ["data"] = new JsonObject { ["label"] = "start" }
                },
                new JsonObject
                {
                    ["id"] = modelNodeId,
                    ["type"] = "model",
                    ["position"] = new JsonObject { ["x"] = 100, ["y"] = 200 },
                    ["data"] = new JsonObject { ["label"] = "model_1", ["provider"] = "OpenAi" }
                },
                new JsonObject
                {
                    ["id"] = endNodeId,
                    ["type"] = "end",
                    ["position"] = new JsonObject { ["x"] = 100, ["y"] = 300 },
                    ["data"] = new JsonObject { ["label"] = "end" }
                }
            },
            ["edges"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["source"] = startNodeId,
                    ["target"] = modelNodeId
                },
                new JsonObject
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["source"] = modelNodeId,
                    ["target"] = endNodeId
                }
            },
            ["viewport"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["zoom"] = 1 }
        };

        var nodeConfigurations = new JsonObject
        {
            [startNodeId] = new JsonObject
            {
                ["type"] = "start",
                ["name"] = "start_1"
            },
            [modelNodeId] = new JsonObject
            {
                ["type"] = "model",
                ["name"] = "model_1",
                ["provider"] = "OpenAi",
                ["modelId"] = "gpt-4",
                ["credentialId"] = credentialId.ToString(),
                ["userMessage"] = "Hello {{input}}"
            },
            [endNodeId] = new JsonObject
            {
                ["type"] = "end",
                ["name"] = "end_1"
            }
        };

        var inputSchema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["input"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray { "input" }
        };

        return new SaveAgentVersionRequestV1
        {
            ReactFlowData = JsonSerializer.Deserialize<JsonElement>(reactFlowData.ToJsonString()),
            NodeConfigurations = JsonSerializer.Deserialize<JsonElement>(nodeConfigurations.ToJsonString()),
            InputSchema = JsonSerializer.Deserialize<JsonElement>(inputSchema.ToJsonString()),
            OutputSchema = null,
            CredentialMappings = new List<CredentialMappingV1>
            {
                new() { NodeId = modelNodeId, CredentialId = credentialId }
            }
        };
    }
}
