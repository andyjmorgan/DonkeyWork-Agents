namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class CreateScheduleResponseV1
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? NextFireTimeUtc { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
