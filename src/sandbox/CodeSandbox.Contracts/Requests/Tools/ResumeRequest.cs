namespace CodeSandbox.Contracts.Requests.Tools;

public class ResumeRequest
{
    public int Pid { get; set; }

    public int TimeoutSeconds { get; set; } = 300;
}
