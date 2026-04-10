using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpBashTool
{
    private const string ToolDescription = """
        Executes a bash command in the sandbox with optional timeout.

        All commands run in the current working directory by default. Use specialized tools (read, edit, write, glob, grep) for file operations instead of shell equivalents.

        If a command times out, you will receive partial output along with the process PID. Use the `resume` tool with that PID to reconnect and retrieve the remaining output.
        """;

    [McpServerTool(Name = "bash"), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The command to execute")] string command,
        [Description("Short description of what this command does")] string? description = null,
        [Description("Timeout in seconds (default 120)")] int timeoutSeconds = 120)
    {
        var response = await executorClient.BashAsync(
            new BashRequest { Command = command, Description = description, TimeoutSeconds = timeoutSeconds },
            ct);

        return response.ToCallToolResult();
    }
}
