using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing research items.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class ResearchTools
{
    private readonly IResearchService _researchService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResearchTools"/> class.
    /// </summary>
    public ResearchTools(IResearchService researchService)
    {
        _researchService = researchService;
    }

    /// <summary>
    /// Lists all research items for the current user.
    /// </summary>
    [McpServerTool(Name = "research_list")]
    [McpTool(
        Name = "research_list",
        Title = "List Research",
        Description = "List all research items for the current user. Research items track investigation topics with a title, plan, result, and associated notes.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<ResearchSummaryV1>> ListResearch(CancellationToken ct)
    {
        return await _researchService.ListAsync(ct);
    }

    /// <summary>
    /// Gets a research item by ID with full details.
    /// </summary>
    [McpServerTool(Name = "research_get")]
    [McpTool(
        Name = "research_get",
        Title = "Get Research",
        Description = "Get a research item by ID with full details including its notes (research material/findings). Use notes_get to read the full content of individual notes.",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<ResearchDetailsV1?> GetResearch(
        [Description("The unique identifier of the research item")] Guid id,
        [Description("Optional character offset to start reading content from (for chunked reading of large content fields)")] int? contentOffset = null,
        [Description("Optional number of characters to read from the offset (for chunked reading of large content fields)")] int? contentLength = null,
        CancellationToken ct = default)
    {
        return await _researchService.GetByIdAsync(id, contentOffset, contentLength, ct);
    }

    /// <summary>
    /// Creates a new research item.
    /// </summary>
    [McpServerTool(Name = "research_create")]
    [McpTool(
        Name = "research_create",
        Title = "Create Research",
        Description = "Create a new research item. Research items track investigation topics. After creating, add notes with notes_create using the researchId to attach research material and findings.",
        Icon = "plus")]
    public async Task<object> CreateResearch(
        [Description("The research title")] string title,
        [Description("The research plan (supports markdown and mermaid diagrams)")] string plan,
        [Description("Status: NotStarted (default), InProgress, Completed, or Cancelled")] ResearchStatus? status,
        CancellationToken ct)
    {
        var request = new CreateResearchRequestV1
        {
            Title = title,
            Plan = plan,
            Status = status ?? ResearchStatus.NotStarted
        };

        var result = await _researchService.CreateAsync(request, ct);
        return new { result.Id, result.Title };
    }

    /// <summary>
    /// Updates an existing research item. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "research_update")]
    [McpTool(
        Name = "research_update",
        Title = "Update Research",
        Description = "Update an existing research item. IMPORTANT: Only `id` is required - all other parameters are optional. Do NOT pass fields you don't intend to change; omitted fields keep their current values automatically. For example, to change only the status, pass just `id` and `status`. When completing, result is required.",
        Icon = "edit")]
    public async Task<object?> UpdateResearch(
        [Description("The unique identifier of the research item to update")] Guid id,
        [Description("New title for the research (omit to keep current)")] string? title = null,
        [Description("New plan (supports markdown and mermaid diagrams, omit to keep current). IMPORTANT: Only provide this if you need to change the plan - the entire content is sent over the wire, so avoid unnecessary updates.")] string? plan = null,
        [Description("Result of the research. MUST be provided when setting status to Completed.")] string? result = null,
        [Description("Status: NotStarted, InProgress, Completed, or Cancelled (omit to keep current)")] ResearchStatus? status = null,
        CancellationToken ct = default)
    {
        // Fetch current research to merge with provided values
        var current = await _researchService.GetByIdAsync(id, ct: ct);
        if (current == null)
        {
            return null;
        }

        var effectiveStatus = status ?? current.Status;
        var effectiveResult = result ?? current.Result;

        if (effectiveStatus is ResearchStatus.Completed && string.IsNullOrWhiteSpace(effectiveResult))
        {
            return new { error = "result is required when setting status to Completed. Please call this tool again with result provided." };
        }

        var request = new UpdateResearchRequestV1
        {
            Title = title ?? current.Title,
            Plan = plan ?? current.Plan,
            Result = effectiveResult,
            Status = effectiveStatus,
        };

        var updated = await _researchService.UpdateAsync(id, request, ct);
        if (updated == null)
        {
            return null;
        }

        return new UpdateAcknowledgmentV1 { Id = id, Status = "updated" };
    }

    /// <summary>
    /// Permanently deletes a research item and all its related data.
    /// </summary>
    [McpServerTool(Name = "research_delete")]
    [McpTool(
        Name = "research_delete",
        Title = "Delete Research",
        Description = "Permanently delete a research item and all its related data (notes, tags)",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public async Task<bool> DeleteResearch(
        [Description("The unique identifier of the research item to delete")] Guid id,
        CancellationToken ct)
    {
        return await _researchService.DeleteAsync(id, ct);
    }
}
