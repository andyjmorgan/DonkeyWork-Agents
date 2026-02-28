using System.ComponentModel;
using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

public sealed class SandboxTools
{
    private readonly SandboxManagerClient _client;

    public SandboxTools(SandboxManagerClient client)
    {
        _client = client;
    }

    [AgentTool("create_sandbox", DisplayName = "Create Sandbox")]
    [Description("Create or find an existing code execution sandbox for this conversation. The sandbox provides an isolated environment to run commands. Call this before execute_command.")]
    public async Task<ToolResult> CreateSandbox(
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var userId = identityContext.UserId.ToString();
        var conversationId = context.ConversationId;

        if (string.IsNullOrEmpty(conversationId))
            return ToolResult.Error("No conversation context available.");

        // Try to find an existing sandbox first
        var existing = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (existing is not null)
        {
            return ToolResult.Json(new
            {
                sandboxId = existing,
                status = "ready",
                message = "Using existing sandbox.",
            });
        }

        // Create a new sandbox
        context.ReportProgress("Creating sandbox...");
        var podName = await _client.CreateSandboxAsync(userId, conversationId, context.ReportProgress, ct);

        return ToolResult.Json(new
        {
            sandboxId = podName,
            status = "ready",
            message = "Sandbox created successfully.",
        });
    }

    [AgentTool("execute_command", DisplayName = "Execute Command")]
    [Description("Execute a bash command in the sandbox. Returns stdout, stderr, exit code, and whether the command timed out. The sandbox must be created first with create_sandbox.")]
    public async Task<ToolResult> ExecuteCommand(
        [Description("The bash command to execute")]
        string command,
        [Description("Timeout in seconds (default 300)")]
        int timeoutSeconds = 300,
        GrainContext context = null!,
        IIdentityContext identityContext = null!,
        CancellationToken ct = default)
    {
        var userId = identityContext.UserId.ToString();
        var conversationId = context.ConversationId;

        if (string.IsNullOrEmpty(conversationId))
            return ToolResult.Error("No conversation context available.");

        // Find the sandbox for this conversation
        var sandboxId = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (sandboxId is null)
            return ToolResult.Error("No sandbox exists for this conversation. Use create_sandbox first.");

        context.ReportProgress("Executing command...");
        var result = await _client.ExecuteCommandAsync(sandboxId, command, timeoutSeconds, ct);

        return ToolResult.Json(new
        {
            stdout = result.Stdout,
            stderr = result.Stderr,
            exitCode = result.ExitCode,
            timedOut = result.TimedOut,
        });
    }

    [AgentTool("delete_sandbox", DisplayName = "Delete Sandbox")]
    [Description("Delete the sandbox for this conversation, freeing its resources.")]
    public async Task<ToolResult> DeleteSandbox(
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct)
    {
        var userId = identityContext.UserId.ToString();
        var conversationId = context.ConversationId;

        if (string.IsNullOrEmpty(conversationId))
            return ToolResult.Error("No conversation context available.");

        var sandboxId = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (sandboxId is null)
            return ToolResult.Success("No sandbox found for this conversation.");

        var deleted = await _client.DeleteSandboxAsync(sandboxId, ct);
        return deleted
            ? ToolResult.Success($"Sandbox '{sandboxId}' deleted successfully.")
            : ToolResult.Error($"Failed to delete sandbox '{sandboxId}'.");
    }
}
