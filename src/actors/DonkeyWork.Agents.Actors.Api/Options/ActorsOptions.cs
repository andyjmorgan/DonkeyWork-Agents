namespace DonkeyWork.Agents.Actors.Api.Options;

public sealed class ActorsOptions
{
    public const string SectionName = "Actors";

    public int ResponseTimeoutMinutes { get; set; } = 25;
}
