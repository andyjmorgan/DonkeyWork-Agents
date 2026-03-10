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

    /// <summary>
    /// Extracts the userId segment from a grain key.
    /// All keys follow the pattern {prefix}{userId}:{rest...}.
    /// </summary>
    public static Guid ExtractUserId(string grainKey)
    {
        var prefixes = new[] { ConversationPrefix, DeepResearchPrefix, DelegatePrefix, ResearchPrefix, TestPrefix };
        foreach (var prefix in prefixes)
        {
            if (!grainKey.StartsWith(prefix))
                continue;

            var rest = grainKey[prefix.Length..];
            var colonIndex = rest.IndexOf(':');
            var userIdSpan = colonIndex >= 0 ? rest[..colonIndex] : rest;
            if (Guid.TryParse(userIdSpan, out var userId))
                return userId;
        }

        return Guid.Empty;
    }
}
