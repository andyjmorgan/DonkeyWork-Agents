using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Actors.Core.Options;

public sealed class SandboxOptions
{
    public const string SectionName = "Sandbox";

    [Required]
    public required string ManagerBaseUrl { get; set; }
}
