using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpWriteTool
{
    private const string ToolDescription = """
        Writes content to a file, creating parent directories as needed. Overwrites the file if it already exists. Prefer editing existing files over writing new ones.
        """;

    [McpServerTool(Name = "write"), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The absolute path to the file to write")] string filePath,
        [Description("The content to write to the file")] string content)
    {
        var response = await executorClient.WriteAsync(
            new WriteRequest { FilePath = filePath, Content = content },
            ct);

        return response.ToCallToolResult();
    }
}
