namespace DonkeyWork.Agents.Actors.Api.EventBus;

/// <summary>
/// Subject naming conventions for the AGENT_EVENTS JetStream stream.
/// Subject pattern: agent.events.{conversationId}.{turnId}
/// </summary>
public static class AgentEventSubjects
{
    public const string StreamName = "AGENT_EVENTS";
    public const string BucketName = "AGENT_EVENT_STASH";
    public const string SubjectsFilter = "agent.events.>";

    public static string ForEvent(string conversationId, Guid turnId)
        => $"agent.events.{conversationId}.{turnId}";

    public static string ForConversation(string conversationId)
        => $"agent.events.{conversationId}.>";
}
