namespace CodeSandbox.Manager.Models;

public class DeleteSandboxResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PodName { get; set; } = string.Empty;
}

public class DeleteAllSandboxesResponse
{
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> DeletedPods { get; set; } = new();
    public List<string> FailedPods { get; set; } = new();
}
