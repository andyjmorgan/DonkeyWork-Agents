namespace CodeSandbox.Manager.Models;

public class ContainerCreationEvent
{
    public string EventType => GetType().Name;
    public string PodName { get; set; } = string.Empty;
}

public class ContainerCreatedEvent : ContainerCreationEvent
{
    public string Phase { get; set; } = string.Empty;
}

public class ContainerWaitingEvent : ContainerCreationEvent
{
    public int AttemptNumber { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ContainerReadyEvent : ContainerCreationEvent
{
    public SandboxInfo? ContainerInfo { get; set; }
    public double ElapsedSeconds { get; set; }
}

public class ContainerFailedEvent : ContainerCreationEvent
{
    public string Reason { get; set; } = string.Empty;
    public SandboxInfo? ContainerInfo { get; set; }
}
