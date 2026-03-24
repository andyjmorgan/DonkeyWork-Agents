using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Notifications.Contracts.Enums;

/// <summary>
/// Types of real-time notifications sent via SignalR.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    // Project notifications
    ProjectCreated,
    ProjectUpdated,
    ProjectDeleted,

    // Milestone notifications
    MilestoneCreated,
    MilestoneUpdated,
    MilestoneDeleted,

    // Task notifications
    TaskCreated,
    TaskUpdated,
    TaskDeleted,

    // Note notifications
    NoteCreated,
    NoteUpdated,
    NoteDeleted,

    // Conversation agent notifications
    ConversationAgentStarted,
    ConversationAgentCompleted
}
