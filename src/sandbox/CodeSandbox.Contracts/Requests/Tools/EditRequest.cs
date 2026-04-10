namespace CodeSandbox.Contracts.Requests.Tools;

public class EditRequest
{
    public string FilePath { get; set; } = string.Empty;

    public string OldString { get; set; } = string.Empty;

    public string NewString { get; set; } = string.Empty;

    public bool ReplaceAll { get; set; }
}
