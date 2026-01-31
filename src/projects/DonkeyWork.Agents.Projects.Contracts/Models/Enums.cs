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
/// Todo status enumeration.
/// </summary>
public enum TodoStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Todo priority enumeration.
/// </summary>
public enum TodoPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
