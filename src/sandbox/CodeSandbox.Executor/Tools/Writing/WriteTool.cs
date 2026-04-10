namespace CodeSandbox.Executor.Tools.Writing;

public class WriteTool
{
    public ToolResult Execute(string filePath, string content)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("filePath must not be empty.", nameof(filePath));
        }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var existed = File.Exists(filePath);

        File.WriteAllText(filePath, content);

        return new ToolResult
        {
            Title = $"Wrote {filePath}",
            Output = existed
                ? $"The file {filePath} has been updated successfully."
                : $"File created successfully at: {filePath}",
        };
    }
}
