namespace Orleans.Streaming.Nats.Configuration;

public class NatsStreamOptions
{
    public string Url { get; set; } = "nats://localhost:4222";
    public string StreamName { get; set; } = "orleans-stream";
    public string SubjectPrefix { get; set; } = "orleans";
    public int Partitions { get; set; } = 8;
    public string ConsumerName { get; set; } = "orleans-consumer";
    public TimeSpan? MaxAge { get; set; }
    public long? MaxBytes { get; set; }
}
