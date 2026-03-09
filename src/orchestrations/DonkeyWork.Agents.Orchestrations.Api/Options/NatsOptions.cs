using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Orchestrations.Api.Options;

public class NatsOptions
{
    public const string SectionName = "Nats";

    [Required]
    public string Url { get; set; } = "nats://localhost:4222";

    public string StreamName { get; set; } = "executions";

    public string SubjectPrefix { get; set; } = "execution";

    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(24);
}
