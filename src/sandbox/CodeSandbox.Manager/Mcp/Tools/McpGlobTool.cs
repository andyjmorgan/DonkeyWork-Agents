using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpGlobTool
{
    private const string ToolDescription = """
        Finds files matching a glob pattern, sorted by modification time (newest first). Supports patterns like "**/*.js" or "src/**/*.ts".
        """;

    [McpServerTool(Name = "glob", ReadOnly = true), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The glob pattern to match files against")] string pattern,
        [Description("The directory to search in (defaults to working directory)")] string? path = null)
    {
        var response = await executorClient.GlobAsync(
            new GlobRequest { Pattern = pattern, Path = path },
            ct);

        return response.ToCallToolResult();
    }
}
