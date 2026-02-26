using System.ComponentModel.DataAnnotations;

namespace CodeSandbox.Manager.Models;

public class CreateSandboxRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ConversationId { get; set; } = string.Empty;

    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    public ResourceRequirements? Resources { get; set; }
}

public class ResourceRequirements
{
    public ResourceValues? Requests { get; set; }
    public ResourceValues? Limits { get; set; }
}

public class ResourceValues
{
    public int? MemoryMi { get; set; }
    public int? CpuMillicores { get; set; }
}
