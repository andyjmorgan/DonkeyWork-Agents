using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools;

public class AgentContractRegistryTests
{
    private readonly Mock<ILogger<AgentContractRegistry>> _logger = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_ScansContractsFromAssembly()
    {
        // Act
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Assert
        Assert.True(registry.HasContract("research"));
        Assert.True(registry.HasContract("deep_research"));
        Assert.True(registry.HasContract("conversation"));
        Assert.True(registry.HasContract("delegate"));
    }

    [Fact]
    public void Constructor_LogsContractCount()
    {
        // Act
        _ = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AgentContractRegistry initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region HasContract Tests

    [Fact]
    public void HasContract_WithExisting_ReturnsTrue()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act & Assert
        Assert.True(registry.HasContract("research"));
    }

    [Fact]
    public void HasContract_WithNonExistent_ReturnsFalse()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act & Assert
        Assert.False(registry.HasContract("nonexistent"));
    }

    [Fact]
    public void HasContract_IsCaseInsensitive()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act & Assert
        Assert.True(registry.HasContract("RESEARCH"));
    }

    #endregion

    #region GetContract Tests

    [Fact]
    public void GetContract_WithExisting_ReturnsDescriptor()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act
        var descriptor = registry.GetContract("research");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("research", descriptor.Name);
        Assert.NotNull(descriptor.Contract);
    }

    [Fact]
    public void GetContract_WithNonExistent_ReturnsNull()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act
        var descriptor = registry.GetContract("nonexistent");

        // Assert
        Assert.Null(descriptor);
    }

    [Fact]
    public void GetContract_ResearchContract_HasCorrectProperties()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act
        var descriptor = registry.GetContract("research");

        // Assert
        Assert.NotNull(descriptor);
        var contract = descriptor.Contract;
        Assert.Contains("research agent", contract.SystemPrompt);
        Assert.NotNull(contract.WebSearch);
        Assert.True(contract.WebSearch.Enabled);
        Assert.Equal("research", contract.AgentType);
    }

    [Fact]
    public void GetContract_ConversationContract_HasSwarmToolGroups()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act
        var descriptor = registry.GetContract("conversation");

        // Assert
        Assert.NotNull(descriptor);
        var contract = descriptor.Contract;
        Assert.Contains("swarm_spawn", contract.ToolGroups);
        Assert.Contains("swarm_management", contract.ToolGroups);
    }

    #endregion

    #region GetAllContracts Tests

    [Fact]
    public void GetAllContracts_ReturnsFourContracts()
    {
        // Arrange
        var registry = new AgentContractRegistry(_logger.Object, typeof(AgentContracts).Assembly);

        // Act
        var contracts = registry.GetAllContracts();

        // Assert
        Assert.True(contracts.Count >= 4);
        Assert.Contains(contracts, c => c.Name == "research");
        Assert.Contains(contracts, c => c.Name == "deep_research");
        Assert.Contains(contracts, c => c.Name == "conversation");
        Assert.Contains(contracts, c => c.Name == "delegate");
    }

    #endregion
}
