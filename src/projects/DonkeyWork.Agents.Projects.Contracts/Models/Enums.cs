namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Project status enumeration.
/// </summary>
public enum ProjectStatus
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>
/// Milestone status enumeration.
/// </summary>
public enum MilestoneStatus
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>
/// Task item status enumeration.
/// </summary>
public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Task item priority enumeration.
/// </summary>
public enum TaskItemPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Research status enumeration.
/// </summary>
public enum ResearchStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
