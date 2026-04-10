using CodeSandbox.Contracts.Responses;
using ModelContextProtocol.Protocol;

namespace CodeSandbox.Manager.Mcp.Tools;

internal static class ToolResponseExtensions
{
    public static CallToolResult ToCallToolResult(this ToolResponse response) => new()
    {
        Content = [new TextContentBlock { Text = response.Output }],
        IsError = response.IsError,
    };
}
