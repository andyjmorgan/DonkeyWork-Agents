using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Actors.Api.Options;

public sealed class ActorsOptions
{
    public const string SectionName = "Actors";

    [Required]
    public string SeaweedFsBaseUrl { get; set; } = "http://localhost:8888";

    public string SeaweedFsBasePath { get; set; } = "/orleans/grain-state";

    public int ResponseTimeoutMinutes { get; set; } = 25;
}
