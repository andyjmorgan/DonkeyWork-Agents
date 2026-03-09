namespace DonkeyWork.Agents.Actors.Contracts.Models;

public static class AgentKeys
{
    public const string ConversationPrefix = "conv:";
    public const string ResearchPrefix = "research:";
    public const string DeepResearchPrefix = "deepresearch:";
    public const string DelegatePrefix = "delegate:";
    public const string TestPrefix = "test:";

    public static string Conversation(Guid userId, Guid conversationId) =>
        $"{ConversationPrefix}{userId}:{conversationId}";

    public static string Test(Guid userId, Guid testId) =>
        $"{TestPrefix}{userId}:{testId}";

    public static string Create(string prefix, Guid userId, Guid conversationId, Guid taskId) =>
        $"{prefix}{userId}:{conversationId}:{taskId}";
}
