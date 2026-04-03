using Xunit;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Services;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Agents.Actors.Tests.Services;

public class ConversationContractHydratorTests
{
    private readonly Mock<IMcpServerConfigurationService> _mcpService = new();
    private readonly Mock<IA2aServerConfigurationService> _a2aService = new();
    private readonly Mock<IAgentDefinitionService> _agentDefService = new();
    private readonly Mock<IOrchestrationService> _orchestrationService = new();
    private readonly ConversationContractHydrator _hydrator;

    private static readonly AgentContract BaseContract = new()
    {
        SystemPrompt = ["You are Navi"],
        ToolGroups = [ToolGroupNames.SwarmDelegate, ToolGroupNames.Sandbox],
        MaxTokens = 20_000,
        DisplayName = "Navi",
    };

    public ConversationContractHydratorTests()
    {
        var logger = new Mock<ILogger<ConversationContractHydrator>>();
        _hydrator = new ConversationContractHydrator(
            _mcpService.Object,
            _a2aService.Object,
            _agentDefService.Object,
            _orchestrationService.Object,
            logger.Object);
    }

    #region HydrateAsync Tests

    [Fact]
    public async Task HydrateAsync_WithNoDiscoveredResources_PreservesBaseContractFields()
    {
        SetupEmptyDiscovery();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Equal(BaseContract.SystemPrompt, result.SystemPrompt);
        Assert.Equal(BaseContract.ToolGroups, result.ToolGroups);
        Assert.Equal(BaseContract.MaxTokens, result.MaxTokens);
        Assert.Equal(BaseContract.DisplayName, result.DisplayName);
        Assert.Empty(result.McpServers);
        Assert.Empty(result.A2aServers);
        Assert.Empty(result.SubAgents);
        Assert.Empty(result.Orchestrations);
    }

    [Fact]
    public async Task HydrateAsync_WithMcpServers_PopulatesMcpServerReferences()
    {
        var httpId = Guid.NewGuid();
        var stdioId = Guid.NewGuid();

        _mcpService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpConnectionConfigV1>
            {
                new() { Id = httpId, Name = "http-server", Description = "HTTP MCP", Endpoint = "http://localhost" },
            });
        _mcpService.Setup(s => s.GetNaviStdioConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpStdioConnectionConfigV1>
            {
                new() { Id = stdioId, Name = "stdio-server", Description = "Stdio MCP", Command = "/bin/server" },
            });
        SetupEmptyA2a();
        SetupEmptyAgents();
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Equal(2, result.McpServers.Length);
        Assert.Equal(httpId.ToString(), result.McpServers[0].Id);
        Assert.Equal("http-server", result.McpServers[0].Name);
        Assert.Equal("HTTP MCP", result.McpServers[0].Description);
        Assert.Equal(stdioId.ToString(), result.McpServers[1].Id);
        Assert.Equal("stdio-server", result.McpServers[1].Name);
    }

    [Fact]
    public async Task HydrateAsync_WithA2aServers_PopulatesA2aServerReferences()
    {
        var a2aId = Guid.NewGuid();

        SetupEmptyMcp();
        _a2aService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<A2aConnectionConfigV1>
            {
                new() { Id = a2aId, Name = "a2a-server", Description = "A2A test", Address = "http://localhost" },
            });
        SetupEmptyAgents();
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Single(result.A2aServers);
        Assert.Equal(a2aId.ToString(), result.A2aServers[0].Id);
        Assert.Equal("a2a-server", result.A2aServers[0].Name);
    }

    [Fact]
    public async Task HydrateAsync_WithSubAgents_PopulatesSubAgentReferences()
    {
        var agentId = Guid.NewGuid();

        SetupEmptyMcp();
        SetupEmptyA2a();
        _agentDefService.Setup(s => s.GetNaviConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NaviAgentDefinitionV1>
            {
                new() { Id = agentId, Name = "researcher", Description = "Research agent" },
            });
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Single(result.SubAgents);
        Assert.Equal(agentId.ToString(), result.SubAgents[0].Id);
        Assert.Equal("researcher", result.SubAgents[0].Name);
        Assert.Equal("Research agent", result.SubAgents[0].Description);
    }

    [Fact]
    public async Task HydrateAsync_WithOrchestrations_PopulatesOrchestrationReferences()
    {
        var orchId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        SetupEmptyMcp();
        SetupEmptyA2a();
        SetupEmptyAgents();
        _orchestrationService.Setup(s => s.ListToolEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolEnabledOrchestrationV1>
            {
                new()
                {
                    Orchestration = new GetOrchestrationResponseV1
                    {
                        Id = orchId,
                        Name = "data-pipeline",
                        Description = "Data pipeline",
                        FriendlyName = "run_pipeline",
                        CreatedAt = DateTimeOffset.UtcNow,
                    },
                    Version = new GetOrchestrationVersionResponseV1
                    {
                        Id = versionId,
                        OrchestrationId = orchId,
                        VersionNumber = 1,
                        IsDraft = false,
                        InputSchema = JsonDocument.Parse("{}").RootElement,
                        ReactFlowData = new ReactFlowData(),
                        NodeConfigurations = JsonDocument.Parse("{}").RootElement,
                        CreatedAt = DateTimeOffset.UtcNow,
                    },
                },
            });

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Single(result.Orchestrations);
        Assert.Equal(orchId.ToString(), result.Orchestrations[0].Id);
        Assert.Equal("data-pipeline", result.Orchestrations[0].Name);
        Assert.Equal("data-pipeline", result.Orchestrations[0].ToolName);
        Assert.Equal(versionId.ToString(), result.Orchestrations[0].VersionId);
    }

    [Fact]
    public async Task HydrateAsync_WithMcpServers_SetsEnableSandboxTrue()
    {
        _mcpService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpConnectionConfigV1>
            {
                new() { Id = Guid.NewGuid(), Name = "server", Endpoint = "http://localhost" },
            });
        _mcpService.Setup(s => s.GetNaviStdioConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpStdioConnectionConfigV1>());
        SetupEmptyA2a();
        SetupEmptyAgents();
        SetupEmptyOrchestrations();

        var contractWithoutSandbox = new AgentContract { ToolGroups = [] };

        var result = await _hydrator.HydrateAsync(contractWithoutSandbox);

        Assert.True(result.EnableSandbox);
    }

    [Fact]
    public async Task HydrateAsync_WithSandboxToolGroup_SetsEnableSandboxTrue()
    {
        SetupEmptyDiscovery();

        var contractWithSandbox = new AgentContract
        {
            ToolGroups = [ToolGroupNames.Sandbox],
        };

        var result = await _hydrator.HydrateAsync(contractWithSandbox);

        Assert.True(result.EnableSandbox);
    }

    [Fact]
    public async Task HydrateAsync_WithNoMcpAndNoSandboxToolGroup_SetsEnableSandboxFalse()
    {
        SetupEmptyDiscovery();

        var contractNoSandbox = new AgentContract { ToolGroups = [] };

        var result = await _hydrator.HydrateAsync(contractNoSandbox);

        Assert.False(result.EnableSandbox);
    }

    [Fact]
    public async Task HydrateAsync_WithSwarmDelegateToolGroup_SetsAllowDelegationTrue()
    {
        SetupEmptyDiscovery();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.True(result.AllowDelegation);
    }

    [Fact]
    public async Task HydrateAsync_WithoutSwarmDelegateToolGroup_SetsAllowDelegationFalse()
    {
        SetupEmptyDiscovery();

        var contractNoDelegation = new AgentContract { ToolGroups = [] };

        var result = await _hydrator.HydrateAsync(contractNoDelegation);

        Assert.False(result.AllowDelegation);
    }

    [Fact]
    public async Task HydrateAsync_WhenMcpServiceThrows_ReturnsEmptyMcpServers()
    {
        _mcpService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MCP service unavailable"));
        _mcpService.Setup(s => s.GetNaviStdioConfigsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MCP service unavailable"));
        SetupEmptyA2a();
        SetupEmptyAgents();
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Empty(result.McpServers);
    }

    [Fact]
    public async Task HydrateAsync_WhenA2aServiceThrows_ReturnsEmptyA2aServers()
    {
        SetupEmptyMcp();
        _a2aService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("A2A service unavailable"));
        SetupEmptyAgents();
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Empty(result.A2aServers);
    }

    [Fact]
    public async Task HydrateAsync_WhenAgentServiceThrows_ReturnsEmptySubAgents()
    {
        SetupEmptyMcp();
        SetupEmptyA2a();
        _agentDefService.Setup(s => s.GetNaviConnectedAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Agent service unavailable"));
        SetupEmptyOrchestrations();

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Empty(result.SubAgents);
    }

    [Fact]
    public async Task HydrateAsync_WhenOrchestrationServiceThrows_ReturnsEmptyOrchestrations()
    {
        SetupEmptyMcp();
        SetupEmptyA2a();
        SetupEmptyAgents();
        _orchestrationService.Setup(s => s.ListToolEnabledAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Orchestration service unavailable"));

        var result = await _hydrator.HydrateAsync(BaseContract);

        Assert.Empty(result.Orchestrations);
    }

    #endregion

    #region Helpers

    private void SetupEmptyDiscovery()
    {
        SetupEmptyMcp();
        SetupEmptyA2a();
        SetupEmptyAgents();
        SetupEmptyOrchestrations();
    }

    private void SetupEmptyMcp()
    {
        _mcpService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpConnectionConfigV1>());
        _mcpService.Setup(s => s.GetNaviStdioConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpStdioConnectionConfigV1>());
    }

    private void SetupEmptyA2a()
    {
        _a2aService.Setup(s => s.GetNaviConnectionConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<A2aConnectionConfigV1>());
    }

    private void SetupEmptyAgents()
    {
        _agentDefService.Setup(s => s.GetNaviConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NaviAgentDefinitionV1>());
    }

    private void SetupEmptyOrchestrations()
    {
        _orchestrationService.Setup(s => s.ListToolEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolEnabledOrchestrationV1>());
    }

    #endregion
}
