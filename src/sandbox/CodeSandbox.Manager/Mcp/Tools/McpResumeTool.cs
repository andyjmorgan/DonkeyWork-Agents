using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpResumeTool
{
    private const string ToolDescription = """
        Reconnects to a previously timed-out bash process to retrieve remaining output. Use the PID from the timeout message.
        """;

    [McpServerTool(Name = "resume", ReadOnly = true), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The process ID (PID) from the timed-out bash command")] int pid,
        [Description("Timeout in seconds to wait for completion (default 300)")] int timeoutSeconds = 300)
    {
        var response = await executorClient.ResumeAsync(
            new ResumeRequest { Pid = pid, TimeoutSeconds = timeoutSeconds },
            ct);

        return response.ToCallToolResult();
    }
}
