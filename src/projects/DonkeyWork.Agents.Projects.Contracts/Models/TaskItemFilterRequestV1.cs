using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Filter parameters for listing task items.
/// </summary>
public sealed class TaskItemFilterRequestV1
{
    /// <summary>
    /// Optional status filter.
    /// </summary>
    [FromQuery(Name = "status")]
    public TaskItemStatus? Status { get; init; }

    /// <summary>
    /// Optional priority filter.
    /// </summary>
    [FromQuery(Name = "priority")]
    public TaskItemPriority? Priority { get; init; }
}
