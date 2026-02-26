using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Orleans.Api.Options;

public sealed class OrleansOptions
{
    public const string SectionName = "Orleans";

    [Required]
    public string SeaweedFsBaseUrl { get; set; } = "http://localhost:8888";

    public string SeaweedFsBasePath { get; set; } = "/orleans/grain-state";

    public int ResponseTimeoutMinutes { get; set; } = 25;
}
