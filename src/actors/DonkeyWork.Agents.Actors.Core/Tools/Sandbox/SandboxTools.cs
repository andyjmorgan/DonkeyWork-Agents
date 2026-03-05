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

    [AgentTool("sandbox_exec", DisplayName = "Sandbox Execute")]
    [Description("Execute a shell command in the sandbox. Returns stdout, stderr, exit code, and whether the command timed out.")]
    public async Task<ToolResult> ExecAsync(
        [Description("The shell command to execute")]
        string command,
        GrainContext context,
        IIdentityContext identityContext,
        CancellationToken ct,
        [Description("Timeout in seconds (default 60). Use longer timeouts for installs or long-running tasks.")]
        int timeout_seconds = 60)
    {
        var userId = identityContext.UserId.ToString();
        var conversationId = context.ConversationId;

        if (string.IsNullOrEmpty(conversationId))
            return ToolResult.Error("No conversation context available.");

        string sandboxId;
        if (context.SandboxHandle is { } handle)
        {
            try
            {
                sandboxId = await handle.Task.WaitAsync(ct);
            }
            catch
            {
                // Eager provisioning failed — fall back to inline provisioning
                sandboxId = await FallbackProvisionAsync(userId, conversationId, context, ct);
            }
        }
        else
        {
            // No handle (defensive backward-compat) — fall back to inline provisioning
            sandboxId = await FallbackProvisionAsync(userId, conversationId, context, ct);
        }

        context.ReportProgress($"sandbox: {Truncate(command, 60)}");

        try
        {
            var result = await _client.ExecuteCommandAsync(sandboxId, command, timeout_seconds, ct);
            var response = ToolResult.Json(new
            {
                stdout = result.Stdout,
                stderr = result.Stderr,
                exitCode = result.ExitCode,
                timedOut = result.TimedOut,
            });
            return result.ExitCode == 0 && !result.TimedOut
                ? response
                : ToolResult.Error(response.Content);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Sandbox execution failed: {ex.Message}");
        }
    }

    private async Task<string> FallbackProvisionAsync(
        string userId, string conversationId, GrainContext context, CancellationToken ct)
    {
        var sandboxId = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (sandboxId is null)
        {
            context.ReportProgress("Creating sandbox...");
            sandboxId = await _client.CreateSandboxAsync(userId, conversationId, context.ReportProgress, ct);
        }
        return sandboxId;
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "...";

    public const string SystemPromptFragment =
        """

        ## Sandbox

        You have access to a code execution sandbox. Use `sandbox_exec` to run shell commands.

        - Commands time out after the specified timeout (default 60s). Use longer timeouts for installs or long-running tasks.
        - Save files the user has requested to `/home/sandbox/files/` — they persist across sandbox restarts and appear on the user's Files page for download. Files outside this directory are lost when the sandbox restarts.
        - GitHub access is available via the `gh` CLI (pre-authenticated).

        ### Skills

        The sandbox has skills at `/home/sandbox/skills/`. To discover available skills:
        `sandbox_exec("ls /home/sandbox/skills/")`

        Before using a skill, read its instructions:
        `sandbox_exec("cat /home/sandbox/skills/{name}/SKILL.md")`

        ### Pre-installed packages

        - Python: requests, numpy, pandas, python-dateutil, pypdf, pdfplumber, reportlab, pytesseract, pdf2image, openpyxl, python-pptx, markitdown[pptx], Pillow, imageio, imageio-ffmpeg
        - Node.js: typescript, ts-node, docx, pptxgenjs (global)
        - System: pandoc, poppler-utils, qpdf, tesseract-ocr, libreoffice
        """;
}
