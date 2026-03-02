namespace CodeSandbox.Manager.Models;

public class McpServerStartFailedEvent : ContainerCreationEvent
{
    public string Reason { get; set; } = string.Empty;
}
