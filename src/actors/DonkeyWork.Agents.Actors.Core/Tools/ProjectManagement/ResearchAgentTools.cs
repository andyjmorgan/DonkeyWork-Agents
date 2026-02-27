using System.ComponentModel;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;

public sealed class ResearchAgentTools
{
    private readonly IResearchService _researchService;

    public ResearchAgentTools(IResearchService researchService)
    {
        _researchService = researchService;
    }

    [AgentTool("research_list", DisplayName = "List Research")]
    [Description("List all research items for the current user.")]
    public async Task<ToolResult> ListResearch(CancellationToken ct = default)
    {
        var research = await _researchService.ListAsync(ct);
        return ToolResult.Json(research);
    }

    [AgentTool("research_get", DisplayName = "Get Research")]
    [Description("Get a research item by ID with full details.")]
    public async Task<ToolResult> GetResearch(
        [Description("The research ID")] Guid researchId,
        [Description("Content offset for chunked reading")] int? contentOffset = null,
        [Description("Content length for chunked reading")] int? contentLength = null,
        CancellationToken ct = default)
    {
        var research = await _researchService.GetByIdAsync(researchId, contentOffset, contentLength, ct);
        return research is not null ? ToolResult.Json(research) : ToolResult.NotFound("Research", researchId);
    }

    [AgentTool("research_create", DisplayName = "Create Research")]
    [Description("Create a new research item.")]
    public async Task<ToolResult> CreateResearch(
        [Description("The research subject")] string subject,
        [Description("The research content")] string? content = null,
        [Description("Research status: NotStarted, InProgress, Completed, Cancelled")] ResearchStatus? status = null,
        CancellationToken ct = default)
    {
        var request = new CreateResearchRequestV1
        {
            Subject = subject,
            Content = content,
            Status = status ?? ResearchStatus.NotStarted,
        };
        var research = await _researchService.CreateAsync(request, ct);
        return ToolResult.Json(research);
    }

    [AgentTool("research_update", DisplayName = "Update Research")]
    [Description("Update an existing research item.")]
    public async Task<ToolResult> UpdateResearch(
        [Description("The research ID")] Guid researchId,
        [Description("The research subject")] string subject,
        [Description("The research content")] string? content = null,
        [Description("A brief summary")] string? summary = null,
        [Description("Research status: NotStarted, InProgress, Completed, Cancelled")] ResearchStatus? status = null,
        [Description("Completion notes")] string? completionNotes = null,
        CancellationToken ct = default)
    {
        var request = new UpdateResearchRequestV1
        {
            Subject = subject,
            Content = content,
            Summary = summary,
            Status = status ?? ResearchStatus.NotStarted,
            CompletionNotes = completionNotes,
        };
        var research = await _researchService.UpdateAsync(researchId, request, ct);
        return research is not null ? ToolResult.Json(research) : ToolResult.NotFound("Research", researchId);
    }

    [AgentTool("research_delete", DisplayName = "Delete Research")]
    [Description("Delete a research item and all its related data.")]
    public async Task<ToolResult> DeleteResearch(
        [Description("The research ID")] Guid researchId,
        CancellationToken ct = default)
    {
        var deleted = await _researchService.DeleteAsync(researchId, ct);
        return deleted
            ? ToolResult.Success($"Research '{researchId}' deleted successfully.")
            : ToolResult.NotFound("Research", researchId);
    }
}
