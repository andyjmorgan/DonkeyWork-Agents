using CodeSandbox.Executor.Tools.Editing.Replacers;

namespace CodeSandbox.Executor.Tools.Editing;

public class EditTool
{
    public ToolResult Execute(string filePath, string oldString, string newString, bool replaceAll = false)
    {
        if (oldString == newString)
        {
            throw new InvalidOperationException("oldString and newString are identical. No changes to apply.");
        }

        if (string.IsNullOrEmpty(oldString))
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, newString);

            return new ToolResult
            {
                Title = $"Created {filePath}",
                Output = $"Created new file with {newString.Length} characters.",
            };
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        var originalContent = File.ReadAllText(filePath);

        var lineEnding = DetectLineEnding(originalContent);
        var normalizedContent = NormalizeLineEndings(originalContent);
        var normalizedOld = NormalizeLineEndings(oldString);
        var normalizedNew = NormalizeLineEndings(newString);

        var replacedContent = ReplaceEngine.Replace(normalizedContent, normalizedOld, normalizedNew, replaceAll);

        var finalContent = ConvertToLineEnding(replacedContent, lineEnding);

        File.WriteAllText(filePath, finalContent);

        return new ToolResult
        {
            Title = $"Edited {filePath}",
            Output = replaceAll
                ? $"The file {filePath} has been updated. All occurrences were successfully replaced."
                : $"The file {filePath} has been updated successfully.",
        };
    }

    public static string DetectLineEnding(string text) =>
        text.Contains("\r\n") ? "\r\n" : "\n";

    public static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n");

    public static string ConvertToLineEnding(string text, string ending) =>
        ending == "\r\n" ? text.Replace("\n", "\r\n") : text;
}
