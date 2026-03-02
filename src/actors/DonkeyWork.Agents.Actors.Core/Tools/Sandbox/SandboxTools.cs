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

        // Auto-provision sandbox on first call
        var sandboxId = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (sandboxId is null)
        {
            context.ReportProgress("Creating sandbox...");
            sandboxId = await _client.CreateSandboxAsync(userId, conversationId, context.ReportProgress, ct);
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

    [AgentTool("sandbox_share", DisplayName = "Share File")]
    [Description("Generate a download link for a file in the user's files directory (/home/sandbox/files/). Use this after saving a file to give the user a clickable download link.")]
    public Task<ToolResult> ShareAsync(
        [Description("The filename to share (e.g. 'report.pdf'). Must be a file in /home/sandbox/files/.")]
        string filename,
        GrainContext context)
    {
        if (string.IsNullOrWhiteSpace(context.SeaweedFsBaseUrl))
            return Task.FromResult(ToolResult.Error("File sharing is not configured for this agent."));

        var cleaned = filename.TrimStart('/');

        // Strip the sandbox files prefix if the caller passed a full path
        const string prefix = "home/sandbox/files/";
        if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[prefix.Length..];

        if (string.IsNullOrWhiteSpace(cleaned))
            return Task.FromResult(ToolResult.Error("Filename cannot be empty."));

        var userId = context.UserId;
        var url = $"{context.SeaweedFsBaseUrl.TrimEnd('/')}/buckets/users/{userId}/files/{Uri.EscapeDataString(cleaned)}";
        return Task.FromResult(ToolResult.Success(url));
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "...";

    public const string SystemPromptFragment =
        """

        ## Sandbox

        You have access to a code execution sandbox. Use `sandbox_exec` to run shell commands.

        - Commands time out after the specified timeout (default 60s). Use longer timeouts for installs or long-running tasks.
        - Only save files the user has explicitly requested to create to `/home/sandbox/files/` — it is only for artifacts. Files outside it are lost when the sandbox restarts.
        - To give the user a download link for a file in `/home/sandbox/files/`, call `sandbox_share` with the filename. It returns a URL the user can click to download the file. Always share files you create for the user.
        - GitHub access is available via the `gh` CLI (pre-authenticated).

        ### Skills

        The sandbox has pre-built skills at `/home/sandbox/skills/`. Before using a skill, read its full instructions: `sandbox_exec("cat /home/sandbox/skills/{name}/SKILL.md")`

        | Skill | What It Does |
        |-------|-------------|
        | docx | Create/read/edit Word documents (.docx) |
        | pdf | Read/create/merge/split/encrypt/OCR PDF files |
        | pptx | Create/read/edit PowerPoint presentations |
        | slack-gif-creator | Animated GIFs optimized for Slack |
        | xlsx | Create/read/edit spreadsheets (.xlsx, .csv, .tsv) |

        ### Pre-installed packages

        - Python: requests, numpy, pandas, python-dateutil, pypdf, pdfplumber, reportlab, pytesseract, pdf2image, openpyxl, python-pptx, markitdown[pptx], Pillow, imageio, imageio-ffmpeg
        - Node.js: typescript, ts-node, docx, pptxgenjs (global)
        - System: pandoc, poppler-utils, qpdf, tesseract-ocr, libreoffice
        """;
}
