namespace DonkeyWork.Agents.Persistence.Entities.Scheduling;

public class ScheduledJobPayloadEntity : BaseEntity
{
    public Guid ScheduledJobId { get; set; }

    public string UserPrompt { get; set; } = string.Empty;

    public string? InputContext { get; set; }

    public int Version { get; set; } = 1;

    public ScheduledJobEntity ScheduledJob { get; set; } = null!;
}
