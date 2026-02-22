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
        Description = "List all research items for the current user. Research items track investigation topics with subject, content, findings summary, and associated notes for research material.",
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
    public async Task<ResearchDetailsV1> CreateResearch(
        [Description("The research subject/question - the original ask")] string subject,
        [Description("Optional detailed content/scope of the research (supports markdown and mermaid diagrams)")] string? content,
        [Description("Status: NotStarted (default), InProgress, Completed, or Cancelled")] ResearchStatus? status,
        CancellationToken ct)
    {
        var request = new CreateResearchRequestV1
        {
            Subject = subject,
            Content = content,
            Status = status ?? ResearchStatus.NotStarted
        };

        return await _researchService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing research item. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "research_update")]
    [McpTool(
        Name = "research_update",
        Title = "Update Research",
        Description = "Update an existing research item. Only provided fields are updated - omit fields to keep their current values. When completing, both summary and completionNotes are required. When cancelling, completionNotes is required.",
        Icon = "edit")]
    public async Task<ResearchDetailsV1?> UpdateResearch(
        [Description("The unique identifier of the research item to update")] Guid id,
        [Description("New subject for the research (omit to keep current)")] string? subject = null,
        [Description("New content/scope (supports markdown and mermaid diagrams, omit to keep current). IMPORTANT: Only provide this if you need to change the content - the entire content is sent over the wire, so avoid unnecessary updates.")] string? content = null,
        [Description("Summary of research findings (required when marking as Completed, omit to keep current)")] string? summary = null,
        [Description("Status: NotStarted, InProgress, Completed, or Cancelled (omit to keep current)")] ResearchStatus? status = null,
        [Description("Completion notes (required when marking as Completed or Cancelled, omit to keep current)")] string? completionNotes = null,
        CancellationToken ct = default)
    {
        // Fetch current research to merge with provided values
        var current = await _researchService.GetByIdAsync(id, ct: ct);
        if (current == null)
        {
            return null;
        }

        var request = new UpdateResearchRequestV1
        {
            Subject = subject ?? current.Subject,
            Content = content ?? current.Content,
            Summary = summary ?? current.Summary,
            Status = status ?? current.Status,
            CompletionNotes = completionNotes ?? current.CompletionNotes
        };

        return await _researchService.UpdateAsync(id, request, ct);
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
