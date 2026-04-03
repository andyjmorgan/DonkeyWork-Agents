namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class UpdateScheduleResponseV1
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? NextFireTimeUtc { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
