using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public static class TestDataBuilder
{
    #region Agent Builders

    public static CreateAgentRequestV1 CreateAgentRequest(string? name = null, string? description = null)
    {
        return new CreateAgentRequestV1
        {
            Name = name ?? $"test-agent-{Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test agent description"
        };
    }

    #endregion

    #region Project Builders

    public static CreateProjectRequestV1 CreateProjectRequest(
        string? name = null,
        string? description = null,
        string? body = null,
        string? successCriteria = null,
        ProjectStatus status = ProjectStatus.NotStarted)
    {
        return new CreateProjectRequestV1
        {
            Name = name ?? $"Test Project {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test project description",
            Body = body,
            SuccessCriteria = successCriteria,
            Status = status
        };
    }

    public static UpdateProjectRequestV1 UpdateProjectRequest(
        string? name = null,
        string? description = null,
        string? body = null,
        string? successCriteria = null,
        ProjectStatus status = ProjectStatus.InProgress)
    {
        return new UpdateProjectRequestV1
        {
            Name = name ?? $"Updated Project {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Updated project description",
            Body = body,
            SuccessCriteria = successCriteria,
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

    public static SaveAgentVersionRequestV1 CreateSaveVersionRequest(
        string? inputSchema = null,
        string? reactFlowData = null,
        string? nodeConfigurations = null)
    {
        var startNodeId = Guid.NewGuid().ToString();
        var endNodeId = Guid.NewGuid().ToString();

        var inputSchemaJson = inputSchema ?? "{}";
        var reactFlowDataJson = reactFlowData ?? $$"""
            {
                "nodes": [
                    { "id": "{{startNodeId}}", "type": "start", "position": { "x": 100, "y": 100 }, "data": {} },
                    { "id": "{{endNodeId}}", "type": "end", "position": { "x": 400, "y": 100 }, "data": {} }
                ],
                "edges": [
                    { "id": "e1", "source": "{{startNodeId}}", "target": "{{endNodeId}}" }
                ],
                "viewport": { "x": 0, "y": 0, "zoom": 1 }
            }
            """;

        // Node configurations must have an entry for each node in the ReactFlow data
        // StartNodeConfiguration requires: name (string), inputSchema (JsonElement)
        // EndNodeConfiguration requires: name (string), outputSchema (optional JsonElement)
        var nodeConfigurationsJson = nodeConfigurations ?? $$"""
            {
                "{{startNodeId}}": {
                    "name": "start-node",
                    "inputSchema": { "type": "object", "properties": { "input": { "type": "string" } } }
                },
                "{{endNodeId}}": {
                    "name": "end-node"
                }
            }
            """;

        return new SaveAgentVersionRequestV1
        {
            InputSchema = JsonDocument.Parse(inputSchemaJson).RootElement.Clone(),
            ReactFlowData = JsonDocument.Parse(reactFlowDataJson).RootElement.Clone(),
            NodeConfigurations = JsonDocument.Parse(nodeConfigurationsJson).RootElement.Clone()
        };
    }

    #endregion

    #region Milestone Builders

    public static CreateMilestoneRequestV1 CreateMilestoneRequest(
        string? name = null,
        string? description = null,
        MilestoneStatus status = MilestoneStatus.NotStarted)
    {
        return new CreateMilestoneRequestV1
        {
            Name = name ?? $"Test Milestone {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test milestone description",
            Status = status
        };
    }

    public static UpdateMilestoneRequestV1 UpdateMilestoneRequest(
        string? name = null,
        string? description = null,
        MilestoneStatus status = MilestoneStatus.InProgress)
    {
        return new UpdateMilestoneRequestV1
        {
            Name = name ?? $"Updated Milestone {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Updated milestone description",
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
