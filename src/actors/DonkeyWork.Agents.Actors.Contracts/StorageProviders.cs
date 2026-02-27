namespace DonkeyWork.Agents.Actors.Contracts;

/// <summary>
/// Well-known Orleans storage provider names used in
/// <c>[PersistentState]</c> attributes and silo configuration.
/// </summary>
public static class StorageProviders
{
    public const string SeaweedFs = "SeaweedFsStore";
    public const string PubSub = "PubSubStore";
}
