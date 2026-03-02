namespace CodeSandbox.Manager.Models;

public class McpServerStartingEvent : ContainerCreationEvent
{
    public string Message { get; set; } = string.Empty;
}
