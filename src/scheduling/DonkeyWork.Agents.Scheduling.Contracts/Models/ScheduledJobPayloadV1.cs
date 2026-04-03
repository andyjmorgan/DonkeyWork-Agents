namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class ScheduledJobPayloadV1
{
    public string UserPrompt { get; set; } = string.Empty;

    public string? InputContext { get; set; }

    public int Version { get; set; }
}
