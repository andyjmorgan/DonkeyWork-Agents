using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Persistence.Entities.Research;
using DonkeyWork.Agents.Projects.Contracts.Models;

// Aliases to resolve ambiguous references between Contracts.Models and Persistence.Entities
using ContractProjectStatus = DonkeyWork.Agents.Projects.Contracts.Models.ProjectStatus;
using ContractMilestoneStatus = DonkeyWork.Agents.Projects.Contracts.Models.MilestoneStatus;
using ContractTaskItemStatus = DonkeyWork.Agents.Projects.Contracts.Models.TaskItemStatus;
using ContractTaskItemPriority = DonkeyWork.Agents.Projects.Contracts.Models.TaskItemPriority;
using EntityProjectStatus = DonkeyWork.Agents.Persistence.Entities.Projects.ProjectStatus;
using EntityMilestoneStatus = DonkeyWork.Agents.Persistence.Entities.Projects.MilestoneStatus;
using EntityTaskItemStatus = DonkeyWork.Agents.Persistence.Entities.Projects.TaskItemStatus;
using EntityTaskItemPriority = DonkeyWork.Agents.Persistence.Entities.Projects.TaskItemPriority;
using EntityResearchStatus = DonkeyWork.Agents.Persistence.Entities.Research.ResearchStatus;

namespace DonkeyWork.Agents.Projects.Core.Tests.Helpers;

/// <summary>
/// Builder class for creating test data.
/// </summary>
public class TestDataBuilder
{
    private readonly Guid _defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    #region Project Builders

    /// <summary>
    /// Creates a basic CreateProjectRequestV1 with default values.
    /// </summary>
    public static CreateProjectRequestV1 CreateProjectRequest(
        string name = "test-project",
        string? content = "Test content")
    {
        return new CreateProjectRequestV1
        {
            Name = name,
            Content = content
        };
    }

    /// <summary>
    /// Creates a ProjectEntity with default test values.
    /// </summary>
    public ProjectEntity CreateProjectEntity(
        Guid? id = null,
        Guid? userId = null,
        string name = "test-project",
        EntityProjectStatus status = EntityProjectStatus.NotStarted)
    {
        return new ProjectEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            Name = name,
            Content = "Test content",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Milestone Builders

    /// <summary>
    /// Creates a basic CreateMilestoneRequestV1 with default values.
    /// </summary>
    public static CreateMilestoneRequestV1 CreateMilestoneRequest(
        string name = "test-milestone",
        string? content = "Test milestone content",
        DateTimeOffset? dueDate = null)
    {
        return new CreateMilestoneRequestV1
        {
            Name = name,
            Content = content,
            DueDate = dueDate
        };
    }

    /// <summary>
    /// Creates a MilestoneEntity with default test values.
    /// </summary>
    public MilestoneEntity CreateMilestoneEntity(
        Guid? id = null,
        Guid? projectId = null,
        Guid? userId = null,
        string name = "test-milestone",
        EntityMilestoneStatus status = EntityMilestoneStatus.NotStarted,
        int sortOrder = 0)
    {
        return new MilestoneEntity
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            Name = name,
            Content = "Test milestone content",
            Status = status,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region TaskItem Builders

    /// <summary>
    /// Creates a basic CreateTaskItemRequestV1 with default values.
    /// </summary>
    public static CreateTaskItemRequestV1 CreateTaskItemRequest(
        string title = "test-task-item",
        string? description = "Test task item description",
        ContractTaskItemPriority priority = ContractTaskItemPriority.Medium,
        Guid? projectId = null,
        Guid? milestoneId = null)
    {
        return new CreateTaskItemRequestV1
        {
            Title = title,
            Description = description,
            Priority = priority,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };
    }

    /// <summary>
    /// Creates a TaskItemEntity with default test values.
    /// </summary>
    public TaskItemEntity CreateTaskItemEntity(
        Guid? id = null,
        Guid? userId = null,
        Guid? projectId = null,
        Guid? milestoneId = null,
        string title = "test-task-item",
        EntityTaskItemStatus status = EntityTaskItemStatus.Pending,
        EntityTaskItemPriority priority = EntityTaskItemPriority.Medium,
        int sortOrder = 0)
    {
        return new TaskItemEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            Title = title,
            Description = "Test task item description",
            Status = status,
            Priority = priority,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Note Builders

    /// <summary>
    /// Creates a basic CreateNoteRequestV1 with default values.
    /// </summary>
    public static CreateNoteRequestV1 CreateNoteRequest(
        string title = "test-note",
        string? content = "Test note content",
        Guid? projectId = null,
        Guid? milestoneId = null)
    {
        return new CreateNoteRequestV1
        {
            Title = title,
            Content = content,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };
    }

    /// <summary>
    /// Creates a NoteEntity with default test values.
    /// </summary>
    public NoteEntity CreateNoteEntity(
        Guid? id = null,
        Guid? userId = null,
        Guid? projectId = null,
        Guid? milestoneId = null,
        string title = "test-note",
        int sortOrder = 0)
    {
        return new NoteEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            Title = title,
            Content = "Test note content",
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Research Builders

    /// <summary>
    /// Creates a basic CreateResearchRequestV1 with default values.
    /// </summary>
    public static CreateResearchRequestV1 CreateResearchRequest(
        string subject = "test-research",
        string? content = "Test research content")
    {
        return new CreateResearchRequestV1
        {
            Subject = subject,
            Content = content
        };
    }

    /// <summary>
    /// Creates a ResearchEntity with default test values.
    /// </summary>
    public ResearchEntity CreateResearchEntity(
        Guid? id = null,
        Guid? userId = null,
        string subject = "test-research",
        EntityResearchStatus status = EntityResearchStatus.NotStarted)
    {
        return new ResearchEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? _defaultUserId,
            Subject = subject,
            Content = "Test research content",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Tag Builders

    /// <summary>
    /// Creates a TagRequestV1 with default values.
    /// </summary>
    public static TagRequestV1 CreateTagRequest(string name = "test-tag", string? color = "#007bff")
    {
        return new TagRequestV1
        {
            Name = name,
            Color = color
        };
    }

    #endregion
}
