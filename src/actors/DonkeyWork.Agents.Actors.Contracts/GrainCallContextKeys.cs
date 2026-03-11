namespace DonkeyWork.Agents.Actors.Contracts;

/// <summary>
/// Well-known keys for Orleans <see cref="Orleans.RequestContext"/>
/// used to propagate caller context into grain call filters.
/// </summary>
public static class GrainCallContextKeys
{
    public const string UserId = "DonkeyWork.UserId";
    public const string ConversationId = "DonkeyWork.ConversationId";
}
