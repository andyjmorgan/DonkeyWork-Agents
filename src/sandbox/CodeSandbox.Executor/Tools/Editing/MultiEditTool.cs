using CodeSandbox.Contracts.Requests.Tools;

namespace CodeSandbox.Executor.Tools.Editing;

public class MultiEditTool
{
    private readonly EditTool editTool;

    public MultiEditTool()
    {
        this.editTool = new EditTool();
    }

    public MultiEditTool(EditTool editTool)
    {
        this.editTool = editTool;
    }

    public ToolResult Execute(string filePath, IReadOnlyList<EditOperation> edits)
    {
        if (edits == null || edits.Count == 0)
        {
            throw new ArgumentException("At least one edit operation is required.", nameof(edits));
        }

        ToolResult? lastResult = null;

        for (int i = 0; i < edits.Count; i++)
        {
            var edit = edits[i];
            try
            {
                lastResult = this.editTool.Execute(filePath, edit.OldString, edit.NewString, edit.ReplaceAll);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Edit {i + 1} of {edits.Count} failed: {ex.Message}", ex);
            }
        }

        return lastResult!;
    }
}
