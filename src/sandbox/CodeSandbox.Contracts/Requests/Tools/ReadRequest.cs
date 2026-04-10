namespace CodeSandbox.Contracts.Requests.Tools;

public class ReadRequest
{
    public string FilePath { get; set; } = string.Empty;

    public int? Offset { get; set; }

    public int? Limit { get; set; }
}
