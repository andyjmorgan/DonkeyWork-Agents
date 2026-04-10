using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpGrepTool
{
    private const string ToolDescription = """
        Searches file contents using regular expressions. Returns matching lines grouped by file, sorted by modification time. Use the include parameter to filter by file pattern.
        """;

    [McpServerTool(Name = "grep", ReadOnly = true), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The regex pattern to search for")] string pattern,
        [Description("The directory to search in (defaults to working directory)")] string? path = null,
        [Description("File pattern filter (e.g. \"*.js\", \"*.{ts,tsx}\")")] string? include = null)
    {
        var response = await executorClient.GrepAsync(
            new GrepRequest { Pattern = pattern, Path = path, Include = include },
            ct);

        return response.ToCallToolResult();
    }
}
