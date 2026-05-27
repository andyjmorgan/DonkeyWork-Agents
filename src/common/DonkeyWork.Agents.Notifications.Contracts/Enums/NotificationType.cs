using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Notifications.Contracts.Enums;

/// <summary>
/// Types of real-time notifications sent via SignalR.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    // Conversation agent notifications
    ConversationAgentStarted,
    ConversationAgentCompleted,
}
