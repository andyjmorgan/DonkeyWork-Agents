using System.Text.Json;

namespace DonkeyWork.Agents.Actors.Core.Tools;

public sealed class ToolResult
{
    public required string Content { get; init; }

    public bool IsError { get; init; }

    public static ToolResult Success(string content) => new() { Content = content };

    public static ToolResult Error(string content) => new() { Content = content, IsError = true };

    public static ToolResult Json<T>(T obj) => new()
    {
        Content = JsonSerializer.Serialize(obj, JsonSerializerOptions.Web),
    };

    public static ToolResult NotFound(string entity, Guid id) => new()
    {
        Content = $"{entity} with ID '{id}' was not found.",
        IsError = true,
    };
}
