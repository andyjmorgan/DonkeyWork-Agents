using System.Net;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

/// <summary>
/// Integration tests for agent execution with different node types.
/// Tests the full execution lifecycle: create agent, save version, execute, verify output, cleanup.
/// </summary>
[Trait("Category", "Integration")]
public class AgentExecutionIntegrationTests : ControllerIntegrationTestBase
{
    private const string AgentsBaseUrl = "/api/v1/orchestrations";

    public AgentExecutionIntegrationTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region MessageFormatter Node Tests

    [Fact]
    public async Task Execute_MessageFormatterNode_ReturnsFormattedMessage()
    {
        // Arrange - Create agent with Start -> MessageFormatter -> End
        var agentId = await CreateAgentAsync("message-formatter-test");
        try
        {
            var startNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"name":{"type":"string"},"greeting":{"type":"string"}},"required":["name"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "formatter_1", DisplayName = "formatter_1" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "formatter_1",
                        template = "Hello, {{Input.name}}! {{Input.greeting ?? 'Welcome!'}}"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act - Execute the agent
            var executeRequest = new { input = new { name = "World" } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Status == ExecutionStatus.Completed, $"Expected Completed but got {result.Status}. Error: {result.Error}");
            Assert.NotNull(result.Output);
            Assert.Contains("Hello, World!", result.Output);
            Assert.Contains("Welcome!", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    [Fact]
    public async Task Execute_MessageFormatterNode_WithAllInputs_ReturnsCompleteMessage()
    {
        // Arrange
        var agentId = await CreateAgentAsync("message-formatter-complete");
        try
        {
            var startNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"firstName":{"type":"string"},"lastName":{"type":"string"},"age":{"type":"integer"}},"required":["firstName","lastName"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "formatter_1", DisplayName = "formatter_1" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "formatter_1",
                        template = "Name: {{Input.firstName}} {{Input.lastName}}, Age: {{Input.age ?? 'unknown'}}"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { firstName = "John", lastName = "Doe", age = 30 } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.Contains("Name: John Doe", result.Output);
            Assert.Contains("Age: 30", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    #endregion

    #region Sleep Node Tests

    [Fact]
    public async Task Execute_SleepNode_CompletesAfterDelay()
    {
        // Arrange
        var agentId = await CreateAgentAsync("sleep-test");
        try
        {
            var startNodeId = Guid.NewGuid();
            var sleepNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{}}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = sleepNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.Sleep, Label = "sleep_1", DisplayName = "sleep_1" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = sleepNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = sleepNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [sleepNodeId.ToString()] = new
                    {
                        type = "Sleep",
                        name = "sleep_1",
                        durationSeconds = 0.1 // 100ms - short for testing
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var executeRequest = new { input = new { } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);
            stopwatch.Stop();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            // Should take at least 100ms due to sleep
            Assert.True(stopwatch.ElapsedMilliseconds >= 90, $"Expected at least 90ms delay, but took {stopwatch.ElapsedMilliseconds}ms");
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    [Fact]
    public async Task Execute_SleepNode_OutputCanBeReferencedByFormatter()
    {
        // Arrange - Test that a formatter can reference sleep node output
        var agentId = await CreateAgentAsync("sleep-formatter-test");
        try
        {
            var startNodeId = Guid.NewGuid();
            var sleepNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"sleepDuration":{"type":"number"}},"required":["sleepDuration"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = sleepNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.Sleep, Label = "sleep_1", DisplayName = "sleep_1" }
                    },
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 400, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "formatter_1", DisplayName = "formatter_1" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = sleepNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = sleepNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [sleepNodeId.ToString()] = new
                    {
                        type = "Sleep",
                        name = "sleep_1",
                        durationSeconds = 0.05 // 50ms - fixed value (variable expressions not supported for numeric fields)
                    },
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "formatter_1",
                        template = "Slept for {{Steps.sleep_1.DurationSeconds}}s"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { sleepDuration = 0.05 } }; // Input not used by sleep node directly
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.Contains("Slept for", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    #endregion

    #region Chained Nodes Tests

    [Fact]
    public async Task Execute_ChainedMessageFormatters_PipesOutputCorrectly()
    {
        // Arrange - Test that output from one node can be used as input to another
        var agentId = await CreateAgentAsync("chained-formatters");
        try
        {
            var startNodeId = Guid.NewGuid();
            var formatter1Id = Guid.NewGuid();
            var formatter2Id = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"value":{"type":"string"}},"required":["value"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = formatter1Id,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 200, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "step1", DisplayName = "step1" }
                    },
                    new ReactFlowNode
                    {
                        Id = formatter2Id,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 350, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "step2", DisplayName = "step2" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = formatter1Id },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatter1Id, Target = formatter2Id },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatter2Id, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [formatter1Id.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "step1",
                        template = "STEP1[{{Input.value}}]"
                    },
                    [formatter2Id.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "step2",
                        template = "STEP2[{{Steps.step1.FormattedMessage}}]"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { value = "original" } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            // The output should show the chained transformation
            Assert.Contains("STEP2[STEP1[original]]", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    [Fact]
    public async Task Execute_SleepThenFormat_ChainsCorrectly()
    {
        // Arrange - Sleep node followed by formatter that references sleep output
        var agentId = await CreateAgentAsync("sleep-then-format");
        try
        {
            var startNodeId = Guid.NewGuid();
            var sleepNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"message":{"type":"string"}},"required":["message"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = sleepNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 200, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.Sleep, Label = "delay", DisplayName = "delay" }
                    },
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 350, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "format", DisplayName = "format" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = sleepNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = sleepNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [sleepNodeId.ToString()] = new
                    {
                        type = "Sleep",
                        name = "delay",
                        durationSeconds = 0.01 // 10ms
                    },
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "format",
                        template = "After {{Steps.delay.DurationSeconds}}s delay: {{Input.message}}"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { message = "Hello after delay!" } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.Contains("After", result.Output);
            Assert.Contains("Hello after delay!", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    #endregion

    #region Simple Pass-Through Tests

    [Fact]
    public async Task Execute_SimpleStartToEnd_ReturnsInput()
    {
        // Arrange - Simplest possible agent: Start -> End
        var agentId = await CreateAgentAsync("simple-passthrough");
        try
        {
            var startNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"data":{"type":"string"}},"required":["data"]}""",
                additionalNodes: [],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>());

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { data = "test value" } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Execute_WithMissingRequiredInput_ExecutesWithNullValues()
    {
        // Arrange - Create agent with required input schema
        var agentId = await CreateAgentAsync("missing-input-test");
        try
        {
            var startNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "formatter_1", DisplayName = "formatter_1" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "formatter_1",
                        template = "Hello, {{Input.name ?? 'default'}}!"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act - Execute without providing the required 'name' input
            var executeRequest = new { input = new { } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);

            // Assert - The template should use the default value
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);
            Assert.NotNull(result);
            Assert.Equal(ExecutionStatus.Completed, result.Status);
            Assert.Contains("Hello, default!", result.Output);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    [Fact]
    public async Task Execute_NonExistentAgent_ReturnsNotFound()
    {
        // Act
        var executeRequest = new { input = new { } };
        var response = await PostResponseAsync($"{AgentsBaseUrl}/{Guid.NewGuid()}/test", executeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Node Execution Trace Tests

    [Fact]
    public async Task Execute_ReturnsNodeExecutionTrace()
    {
        // Arrange
        var agentId = await CreateAgentAsync("trace-test");
        try
        {
            var startNodeId = Guid.NewGuid();
            var formatterNodeId = Guid.NewGuid();
            var endNodeId = Guid.NewGuid();

            var versionRequest = CreateVersionWithNodes(
                startNodeId,
                endNodeId,
                inputSchema: """{"type":"object","properties":{"value":{"type":"string"}},"required":["value"]}""",
                additionalNodes:
                [
                    new ReactFlowNode
                    {
                        Id = formatterNodeId,
                        Type = "schemaNode",
                        Position = new ReactFlowPosition { X = 250, Y = 100 },
                        Data = new ReactFlowNodeData { NodeType = NodeType.MessageFormatter, Label = "formatter", DisplayName = "formatter" }
                    }
                ],
                additionalEdges:
                [
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = formatterNodeId },
                    new ReactFlowEdge { Id = Guid.NewGuid(), Source = formatterNodeId, Target = endNodeId }
                ],
                additionalNodeConfigs: new Dictionary<string, object>
                {
                    [formatterNodeId.ToString()] = new
                    {
                        type = "MessageFormatter",
                        name = "formatter",
                        template = "Result: {{Input.value}}"
                    }
                });

            await SaveVersionAsync(agentId, versionRequest);

            // Act
            var executeRequest = new { input = new { value = "test" } };
            var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/test", executeRequest);
            var result = await response.Content.ReadFromJsonAsync<ExecuteOrchestrationResponseV1>(JsonOptions);

            var nodeExecutionsResponse = await GetAsync<GetNodeExecutionsResponseV1>($"{AgentsBaseUrl}/executions/{result!.ExecutionId}/nodes");

            // Assert
            Assert.NotNull(nodeExecutionsResponse);
            Assert.True(nodeExecutionsResponse.NodeExecutions.Count >= 3, "Should have at least 3 node executions (start, formatter, end)");

            // Verify each node type is represented
            var nodeTypes = nodeExecutionsResponse.NodeExecutions.Select(n => n.NodeType).ToList();
            Assert.Contains(NodeType.Start, nodeTypes);
            Assert.Contains(NodeType.MessageFormatter, nodeTypes);
            Assert.Contains(NodeType.End, nodeTypes);
        }
        finally
        {
            await DeleteAgentAsync(agentId);
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateAgentAsync(string name)
    {
        var request = TestDataBuilder.CreateAgentRequest(name);
        var response = await PostAsync<CreateOrchestrationResponseV1>(AgentsBaseUrl, request);
        return response!.Id;
    }

    private async Task SaveVersionAsync(Guid agentId, SaveOrchestrationVersionRequestV1 versionRequest)
    {
        var response = await PostResponseAsync($"{AgentsBaseUrl}/{agentId}/versions", versionRequest);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAgentAsync(Guid agentId)
    {
        await DeleteAsync($"{AgentsBaseUrl}/{agentId}");
    }

    private static SaveOrchestrationVersionRequestV1 CreateVersionWithNodes(
        Guid startNodeId,
        Guid endNodeId,
        string inputSchema,
        IEnumerable<ReactFlowNode> additionalNodes,
        IEnumerable<ReactFlowEdge> additionalEdges,
        Dictionary<string, object> additionalNodeConfigs)
    {
        var nodes = new List<ReactFlowNode>
        {
            new()
            {
                Id = startNodeId,
                Type = "schemaNode",
                Position = new ReactFlowPosition { X = 100, Y = 100 },
                Data = new ReactFlowNodeData { NodeType = NodeType.Start, Label = "start_1", DisplayName = "start_1" }
            },
            new()
            {
                Id = endNodeId,
                Type = "schemaNode",
                Position = new ReactFlowPosition { X = 500, Y = 100 },
                Data = new ReactFlowNodeData { NodeType = NodeType.End, Label = "end_1", DisplayName = "end_1" }
            }
        };
        nodes.AddRange(additionalNodes);

        var edges = additionalEdges.ToList();

        var reactFlowData = new ReactFlowData
        {
            Nodes = nodes,
            Edges = edges,
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigs = new Dictionary<string, object>
        {
            [startNodeId.ToString()] = new
            {
                type = "Start",
                name = "start_1",
                inputSchema = JsonDocument.Parse(inputSchema).RootElement
            },
            [endNodeId.ToString()] = new
            {
                type = "End",
                name = "end_1"
            }
        };

        foreach (var (key, value) in additionalNodeConfigs)
        {
            nodeConfigs[key] = value;
        }

        var nodeConfigsJson = JsonSerializer.Serialize(nodeConfigs);

        return new SaveOrchestrationVersionRequestV1
        {
            InputSchema = JsonDocument.Parse(inputSchema),
            ReactFlowData = reactFlowData,
            NodeConfigurations = JsonDocument.Parse(nodeConfigsJson).RootElement.Clone(),
            Interface = new DirectInterfaceConfig()
        };
    }

    #endregion
}
