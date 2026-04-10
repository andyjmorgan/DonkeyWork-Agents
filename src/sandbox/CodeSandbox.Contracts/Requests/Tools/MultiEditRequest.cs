namespace CodeSandbox.Contracts.Requests.Tools;

public class MultiEditRequest
{
    public string FilePath { get; set; } = string.Empty;

    public List<EditOperation> Edits { get; set; } = [];
}
