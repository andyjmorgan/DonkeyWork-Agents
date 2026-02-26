using System.Text.Json;

namespace DonkeyWork.Agents.Orleans.Core.Middleware;

internal interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct);
}

internal record ToolExecutionResult(string Content, bool IsError = false);
