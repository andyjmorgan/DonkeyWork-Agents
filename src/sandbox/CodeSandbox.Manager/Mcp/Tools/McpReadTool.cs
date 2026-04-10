using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpReadTool
{
    private const string ToolDescription = """
        Reads a file or directory from the sandbox filesystem.

        Returns line-numbered content for files, or entry listings for directories. By default reads up to 2000 lines. Use offset/limit for pagination on large files.
        """;

    [McpServerTool(Name = "read", ReadOnly = true), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The absolute path to the file or directory to read")] string filePath,
        [Description("The line number to start reading from (1-indexed)")] int? offset = null,
        [Description("The maximum number of lines to read (defaults to 2000)")] int? limit = null)
    {
        var response = await executorClient.ReadAsync(
            new ReadRequest { FilePath = filePath, Offset = offset, Limit = limit },
            ct);

        return response.ToCallToolResult();
    }
}
