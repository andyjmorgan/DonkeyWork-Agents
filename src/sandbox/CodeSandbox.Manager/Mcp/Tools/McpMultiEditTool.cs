using System.ComponentModel;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Manager.Services.Executor;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeSandbox.Manager.Mcp.Tools;

[McpServerToolType]
public static class McpMultiEditTool
{
    private const string ToolDescription = """
        Performs multiple sequential edit operations on a single file. Each edit operates on the result of the previous edit. Prefer this over multiple single edits to the same file.
        """;

    [McpServerTool(Name = "multiedit"), Description(ToolDescription)]
    public static async Task<CallToolResult> Execute(
        IExecutorClient executorClient,
        CancellationToken ct,
        [Description("The absolute path to the file to modify")] string filePath,
        [Description("Array of edit operations to perform sequentially")] List<EditOperation> edits)
    {
        if (edits.Count == 0)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = "Error: at least one edit operation is required" }],
                IsError = true,
            };
        }

        var response = await executorClient.MultiEditAsync(
            new MultiEditRequest { FilePath = filePath, Edits = edits },
            ct);

        return response.ToCallToolResult();
    }
}
