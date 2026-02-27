using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.ProjectManagement;

public class MilestoneAgentToolsTests
{
    private readonly Mock<IMilestoneService> _milestoneService = new();
    private readonly MilestoneAgentTools _tools;

    public MilestoneAgentToolsTests()
    {
        _tools = new MilestoneAgentTools(_milestoneService.Object);
    }

    #region ListMilestones Tests

    [Fact]
    public async Task ListMilestones_ReturnsJsonResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var milestones = new List<MilestoneSummaryV1>
        {
            new() { Id = Guid.NewGuid(), ProjectId = projectId, Name = "M1" },
        };
        _milestoneService.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);

        // Act
        var result = await _tools.ListMilestones(projectId);

        // Assert
        Assert.False(result.IsError);
        var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(result.Content);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
    }

    #endregion

    #region GetMilestone Tests

    [Fact]
    public async Task GetMilestone_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        var milestone = new MilestoneDetailsV1 { Id = milestoneId, Name = "Test Milestone" };
        _milestoneService.Setup(x => x.GetByIdAsync(milestoneId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestone);

        // Act
        var result = await _tools.GetMilestone(milestoneId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Test Milestone", result.Content);
    }

    [Fact]
    public async Task GetMilestone_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _milestoneService.Setup(x => x.GetByIdAsync(milestoneId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MilestoneDetailsV1?)null);

        // Act
        var result = await _tools.GetMilestone(milestoneId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region CreateMilestone Tests

    [Fact]
    public async Task CreateMilestone_DelegatesToService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var created = new MilestoneDetailsV1 { Id = Guid.NewGuid(), ProjectId = projectId, Name = "New" };
        _milestoneService.Setup(x => x.CreateAsync(projectId, It.IsAny<CreateMilestoneRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _tools.CreateMilestone(projectId, "New", "content", "criteria", MilestoneStatus.InProgress);

        // Assert
        Assert.False(result.IsError);
        _milestoneService.Verify(x => x.CreateAsync(
            projectId,
            It.Is<CreateMilestoneRequestV1>(r => r.Name == "New" && r.Content == "content" && r.SuccessCriteria == "criteria"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMilestone_WhenProjectNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _milestoneService.Setup(x => x.CreateAsync(projectId, It.IsAny<CreateMilestoneRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MilestoneDetailsV1?)null);

        // Act
        var result = await _tools.CreateMilestone(projectId, "New");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region UpdateMilestone Tests

    [Fact]
    public async Task UpdateMilestone_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        var updated = new MilestoneDetailsV1 { Id = milestoneId, Name = "Updated" };
        _milestoneService.Setup(x => x.UpdateAsync(milestoneId, It.IsAny<UpdateMilestoneRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _tools.UpdateMilestone(milestoneId, "Updated");

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Updated", result.Content);
    }

    [Fact]
    public async Task UpdateMilestone_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _milestoneService.Setup(x => x.UpdateAsync(milestoneId, It.IsAny<UpdateMilestoneRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MilestoneDetailsV1?)null);

        // Act
        var result = await _tools.UpdateMilestone(milestoneId, "Updated");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DeleteMilestone Tests

    [Fact]
    public async Task DeleteMilestone_WhenDeleted_ReturnsSuccess()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _milestoneService.Setup(x => x.DeleteAsync(milestoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.DeleteMilestone(milestoneId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("deleted successfully", result.Content);
    }

    [Fact]
    public async Task DeleteMilestone_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _milestoneService.Setup(x => x.DeleteAsync(milestoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tools.DeleteMilestone(milestoneId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion
}
