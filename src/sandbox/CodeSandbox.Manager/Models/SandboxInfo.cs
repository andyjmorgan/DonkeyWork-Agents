namespace CodeSandbox.Manager.Models;

public class SandboxInfo
{
    public string Name { get; set; } = string.Empty;
    public string Phase { get; set; } = "Unknown";
    public bool IsReady { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? NodeName { get; set; }
    public string? PodIP { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public string? Image { get; set; }
    public DateTime? LastActivity { get; set; }
}
