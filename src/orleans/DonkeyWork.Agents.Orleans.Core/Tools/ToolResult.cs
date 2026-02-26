namespace DonkeyWork.Agents.Orleans.Core.Tools;

public sealed class ToolResult
{
    public required string Content { get; init; }

    public bool IsError { get; init; }

    public static ToolResult Success(string content) => new() { Content = content };

    public static ToolResult Error(string content) => new() { Content = content, IsError = true };
}
