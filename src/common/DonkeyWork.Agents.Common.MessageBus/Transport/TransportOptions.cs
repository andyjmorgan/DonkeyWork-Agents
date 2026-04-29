namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class TransportOptions
{
    public string NatsUrl { get; set; } = "nats://localhost:4222";
    public string Stream { get; set; } = "AGENT_EVENTS";
    public string Subject { get; set; } = "agent.events";
    public string SubjectFilter { get; set; } = "agent.events.>";
    public string Consumer { get; set; } = "agent-events-consumer";
    public string Bucket { get; set; } = "AGENT_EVENT_STASH";
    public string Serializer { get; set; } = "MessagePack";
    public int StashThresholdBytes { get; set; } = 786432;
    public int StreamMaxAgeMinutes { get; set; } = 60;
    public int BucketTtlMinutes { get; set; } = 75;
    public int Replicas { get; set; } = 1;
    public int RequestTimeoutSeconds { get; set; } = 10;
    public int CommandTimeoutSeconds { get; set; } = 10;
    public int ConnectTimeoutSeconds { get; set; } = 2;
    public int PingIntervalSeconds { get; set; } = 5;
    public int MaxPingOut { get; set; } = 2;
    public int ReconnectWaitMinMs { get; set; } = 250;
    public int ReconnectWaitMaxMs { get; set; } = 2000;
    public int MaxDeliver { get; set; } = 5;
    public int AckWaitSeconds { get; set; } = 30;
    public int MaxMsgsPerSubject { get; set; } = 10000;
}
