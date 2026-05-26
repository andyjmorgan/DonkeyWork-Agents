using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public static class TestDataBuilder
{
    #region Audio Collection Builders

    public static CreateAudioCollectionRequestV1 CreateAudioCollectionRequest(
        string? name = null,
        string? description = null,
        string? defaultVoice = null,
        string? defaultModel = null)
    {
        return new CreateAudioCollectionRequestV1
        {
            Name = name ?? $"Test Collection {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test collection description",
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
        };
    }

    public static UpdateAudioCollectionRequestV1 UpdateAudioCollectionRequest(
        string? name = null,
        string? description = null,
        string? defaultVoice = null,
        string? defaultModel = null,
        string? coverImagePath = null)
    {
        return new UpdateAudioCollectionRequestV1
        {
            Name = name,
            Description = description,
            DefaultVoice = defaultVoice,
            DefaultModel = defaultModel,
            CoverImagePath = coverImagePath,
        };
    }

    public static MoveRecordingToCollectionRequestV1 MoveRecordingRequest(
        Guid? collectionId,
        int? sequenceNumber = null,
        string? chapterTitle = null)
    {
        return new MoveRecordingToCollectionRequestV1
        {
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = chapterTitle,
        };
    }

    #endregion

    #region Agent Builders

    public static CreateOrchestrationRequestV1 CreateAgentRequest(string? name = null, string? description = null)
    {
        return new CreateOrchestrationRequestV1
        {
            Name = name ?? $"test-agent-{Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test agent description"
        };
    }

    #endregion

    #region API Key Builders

    public static CreateApiKeyRequestV1 CreateApiKeyRequest(string? name = null, string? description = null)
    {
        return new CreateApiKeyRequestV1
        {
            Name = name ?? $"test-api-key-{Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test API key description"
        };
    }

    #endregion

    #region External API Key Builders

    public static CreateExternalApiKeyRequestV1 CreateExternalApiKeyRequest(
        ExternalApiKeyProvider provider = ExternalApiKeyProvider.OpenAI,
        string? name = null,
        string? apiKey = null)
    {
        return new CreateExternalApiKeyRequestV1
        {
            Provider = provider,
            Name = name ?? $"test-{provider.ToString().ToLower()}-key-{Guid.NewGuid().ToString("N")[..8]}",
            ApiKey = apiKey ?? $"sk-test-{Guid.NewGuid():N}"
        };
    }

    #endregion

    #region Agent Version Builders

    public static SaveOrchestrationVersionRequestV1 CreateSaveVersionRequest(
        string? inputSchema = null,
        string? reactFlowData = null,
        string? nodeConfigurations = null)
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var inputSchemaJson = inputSchema ?? "{}";

        ReactFlowData parsedReactFlowData;
        if (reactFlowData != null)
        {
            parsedReactFlowData = JsonSerializer.Deserialize<ReactFlowData>(reactFlowData) ?? new ReactFlowData();
        }
        else
        {
            parsedReactFlowData = new ReactFlowData
            {
                Nodes =
                [
                    new ReactFlowNode
                    {
                        Id = startNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 100, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.Start, Label = "start_1", DisplayName = "start_1" }
                    },
                    new ReactFlowNode
                    {
                        Id = endNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 400, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.End, Label = "end_1", DisplayName = "end_1" }
                    }
                ],
                Edges =
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = endNodeId }
                ],
                Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
            };
        }

        // Node configurations must have an entry for each node in the ReactFlow data
        // StartNodeConfiguration requires: name (string), inputSchema (JsonElement)
        // EndNodeConfiguration requires: name (string), outputSchema (optional JsonElement)
        var nodeConfigurationsJson = nodeConfigurations ?? $$"""
            {
                "{{startNodeId}}": {
                    "type": "Start",
                    "name": "start-node",
                    "inputSchema": { "type": "object", "properties": { "input": { "type": "string" } } }
                },
                "{{endNodeId}}": {
                    "type": "End",
                    "name": "end-node"
                }
            }
            """;

        return new SaveOrchestrationVersionRequestV1
        {
            InputSchema = JsonDocument.Parse(inputSchemaJson),
            ReactFlowData = parsedReactFlowData,
            NodeConfigurations = JsonDocument.Parse(nodeConfigurationsJson).RootElement.Clone(),
            DirectEnabled = true
        };
    }

    #endregion

    #region MCP Server Builders

    public static CreateMcpServerRequestV1 CreateMcpStdioServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? command = null,
        List<string>? arguments = null)
    {
        return new CreateMcpServerRequestV1
        {
            Name = name ?? $"Test MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test MCP server description",
            TransportType = McpTransportType.Stdio,
            IsEnabled = isEnabled,
            StdioConfiguration = new CreateMcpStdioConfigurationRequestV1
            {
                Command = command ?? "npx",
                Arguments = arguments ?? ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
            }
        };
    }

    public static CreateMcpServerRequestV1 CreateMcpHttpServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? endpoint = null,
        McpHttpTransportMode transportMode = McpHttpTransportMode.AutoDetect,
        McpHttpAuthType authType = McpHttpAuthType.None)
    {
        return new CreateMcpServerRequestV1
        {
            Name = name ?? $"Test MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test MCP server description",
            TransportType = McpTransportType.Http,
            IsEnabled = isEnabled,
            HttpConfiguration = new CreateMcpHttpConfigurationRequestV1
            {
                Endpoint = endpoint ?? "https://example.com/mcp",
                TransportMode = transportMode,
                AuthType = authType
            }
        };
    }

    public static UpdateMcpServerRequestV1 UpdateMcpStdioServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? command = null)
    {
        return new UpdateMcpServerRequestV1
        {
            Name = name ?? $"Updated MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Updated MCP server description",
            TransportType = McpTransportType.Stdio,
            IsEnabled = isEnabled,
            StdioConfiguration = new CreateMcpStdioConfigurationRequestV1
            {
                Command = command ?? "python"
            }
        };
    }

    #endregion
}
