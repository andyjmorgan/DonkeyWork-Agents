using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.ProjectManagement;

public class ResearchAgentToolsTests
{
    private readonly Mock<IResearchService> _researchService = new();
    private readonly ResearchAgentTools _tools;

    public ResearchAgentToolsTests()
    {
        _tools = new ResearchAgentTools(_researchService.Object);
    }

    #region ListResearch Tests

    [Fact]
    public async Task ListResearch_ReturnsJsonResult()
    {
        // Arrange
        _researchService.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResearchSummaryV1> { new() { Id = Guid.NewGuid(), Subject = "Research 1" } });

        // Act
        var result = await _tools.ListResearch();

        // Assert
        Assert.False(result.IsError);
        var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(result.Content);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
    }

    #endregion

    #region GetResearch Tests

    [Fact]
    public async Task GetResearch_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        var research = new ResearchDetailsV1 { Id = researchId, Subject = "Test Research" };
        _researchService.Setup(x => x.GetByIdAsync(researchId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(research);

        // Act
        var result = await _tools.GetResearch(researchId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Test Research", result.Content);
    }

    [Fact]
    public async Task GetResearch_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        _researchService.Setup(x => x.GetByIdAsync(researchId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResearchDetailsV1?)null);

        // Act
        var result = await _tools.GetResearch(researchId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region CreateResearch Tests

    [Fact]
    public async Task CreateResearch_DelegatesToService()
    {
        // Arrange
        var created = new ResearchDetailsV1 { Id = Guid.NewGuid(), Subject = "New Research" };
        _researchService.Setup(x => x.CreateAsync(It.IsAny<CreateResearchRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _tools.CreateResearch("New Research", "Content", ResearchStatus.InProgress);

        // Assert
        Assert.False(result.IsError);
        _researchService.Verify(x => x.CreateAsync(
            It.Is<CreateResearchRequestV1>(r => r.Subject == "New Research" && r.Content == "Content" && r.Status == ResearchStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateResearch Tests

    [Fact]
    public async Task UpdateResearch_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        var updated = new ResearchDetailsV1 { Id = researchId, Subject = "Updated" };
        _researchService.Setup(x => x.UpdateAsync(researchId, It.IsAny<UpdateResearchRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _tools.UpdateResearch(researchId, "Updated", status: ResearchStatus.Completed);

        // Assert
        Assert.False(result.IsError);
        _researchService.Verify(x => x.UpdateAsync(
            researchId,
            It.Is<UpdateResearchRequestV1>(r => r.Subject == "Updated" && r.Status == ResearchStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateResearch_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        _researchService.Setup(x => x.UpdateAsync(researchId, It.IsAny<UpdateResearchRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResearchDetailsV1?)null);

        // Act
        var result = await _tools.UpdateResearch(researchId, "Updated");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DeleteResearch Tests

    [Fact]
    public async Task DeleteResearch_WhenDeleted_ReturnsSuccess()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        _researchService.Setup(x => x.DeleteAsync(researchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.DeleteResearch(researchId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("deleted successfully", result.Content);
    }

    [Fact]
    public async Task DeleteResearch_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        _researchService.Setup(x => x.DeleteAsync(researchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tools.DeleteResearch(researchId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion
}
