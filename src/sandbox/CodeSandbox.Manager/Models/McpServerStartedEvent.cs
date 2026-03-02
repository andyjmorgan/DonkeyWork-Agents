namespace CodeSandbox.Manager.Models;

public class McpServerStartedEvent : ContainerCreationEvent
{
    public McpServerInfo? ServerInfo { get; set; }
    public double ElapsedSeconds { get; set; }
}
