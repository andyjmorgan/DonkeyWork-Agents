using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public static class TestDataBuilder
{
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

    #region Project Builders

    public static CreateProjectRequestV1 CreateProjectRequest(
        string? name = null,
        string? content = null,
        ProjectStatus status = ProjectStatus.NotStarted)
    {
        return new CreateProjectRequestV1
        {
            Name = name ?? $"Test Project {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Test project content",
            Status = status
        };
    }

    public static UpdateProjectRequestV1 UpdateProjectRequest(
        string? name = null,
        string? content = null,
        ProjectStatus status = ProjectStatus.InProgress)
    {
        return new UpdateProjectRequestV1
        {
            Name = name ?? $"Updated Project {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Updated project content",
            Status = status
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
            Interface = new DirectInterfaceConfig()
        };
    }

    #endregion

    #region Milestone Builders

    public static CreateMilestoneRequestV1 CreateMilestoneRequest(
        string? name = null,
        string? content = null,
        MilestoneStatus status = MilestoneStatus.NotStarted)
    {
        return new CreateMilestoneRequestV1
        {
            Name = name ?? $"Test Milestone {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Test milestone content",
            Status = status
        };
    }

    public static UpdateMilestoneRequestV1 UpdateMilestoneRequest(
        string? name = null,
        string? content = null,
        MilestoneStatus status = MilestoneStatus.InProgress)
    {
        return new UpdateMilestoneRequestV1
        {
            Name = name ?? $"Updated Milestone {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Updated milestone content",
            Status = status
        };
    }

    #endregion

    #region Todo Builders

    public static CreateTodoRequestV1 CreateTodoRequest(
        string? title = null,
        string? description = null,
        TodoStatus status = TodoStatus.Pending,
        TodoPriority priority = TodoPriority.Medium,
        Guid? projectId = null,
        Guid? milestoneId = null)
    {
        return new CreateTodoRequestV1
        {
            Title = title ?? $"Test Todo {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test todo description",
            Status = status,
            Priority = priority,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };
    }

    public static UpdateTodoRequestV1 UpdateTodoRequest(
        string? title = null,
        string? description = null,
        TodoStatus status = TodoStatus.InProgress,
        TodoPriority priority = TodoPriority.High)
    {
        return new UpdateTodoRequestV1
        {
            Title = title ?? $"Updated Todo {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Updated todo description",
            Status = status,
            Priority = priority
        };
    }

    #endregion

    #region Note Builders

    public static CreateNoteRequestV1 CreateNoteRequest(
        string? title = null,
        string? content = null,
        Guid? projectId = null,
        Guid? milestoneId = null)
    {
        return new CreateNoteRequestV1
        {
            Title = title ?? $"Test Note {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Test note content",
            ProjectId = projectId,
            MilestoneId = milestoneId
        };
    }

    public static UpdateNoteRequestV1 UpdateNoteRequest(
        string? title = null,
        string? content = null)
    {
        return new UpdateNoteRequestV1
        {
            Title = title ?? $"Updated Note {Guid.NewGuid().ToString("N")[..8]}",
            Content = content ?? "Updated note content"
        };
    }

    #endregion
}
