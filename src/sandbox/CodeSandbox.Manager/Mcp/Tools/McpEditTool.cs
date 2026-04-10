using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpEditTool
{
    private const string ToolDescription = """
        Performs exact string replacements in files.

        The edit will fail if oldString is not found or if multiple matches exist (unless replaceAll is true). Use replaceAll for renaming variables or replacing repeated strings across the file.
        """;

    [McpServerTool(Name = "edit"), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The absolute path to the file to modify")] string filePath,
        [Description("The text to replace")] string oldString,
        [Description("The replacement text")] string newString,
        [Description("Replace all occurrences (default false)")] bool? replaceAll = null)
    {
        var response = await executorClient.EditAsync(
            new EditRequest { FilePath = filePath, OldString = oldString, NewString = newString, ReplaceAll = replaceAll ?? false },
            ct);

        return response.ToCallToolResult();
    }
}
