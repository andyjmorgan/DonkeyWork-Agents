using System.ComponentModel;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

public sealed class SandboxTools
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1)];

    private readonly SandboxManagerClient _client;
    private readonly ISandboxCredentialMappingService? _credentialMappingService;
    private readonly ILogger<SandboxTools> _logger;

    public SandboxTools(SandboxManagerClient client, ILogger<SandboxTools> logger, ISandboxCredentialMappingService? credentialMappingService = null)
    {
        _client = client;
        _credentialMappingService = credentialMappingService;
        _logger = logger;
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

        for (var attempt = 0; ; attempt++)
        {
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
            catch (HttpRequestException ex) when (attempt < MaxRetries && IsTransient(ex.StatusCode))
            {
                _logger.LogWarning(ex,
                    "Transient HTTP error on sandbox_exec attempt {Attempt}/{MaxRetries} (status={Status}), retrying",
                    attempt + 1, MaxRetries, ex.StatusCode);
                await Task.Delay(RetryDelays[attempt], ct);
            }
            catch (Exception ex)
            {
                return ToolResult.Error($"Sandbox execution failed: {ex.Message}");
            }
        }
    }

    private async Task<string> FallbackProvisionAsync(
        string userId, string conversationId, GrainContext context, CancellationToken ct)
    {
        var sandboxId = await _client.FindSandboxAsync(userId, conversationId, ct);
        if (sandboxId is null)
        {
            // Resolve credential domains so the auth proxy can inject tokens
            IReadOnlyList<string>? credentialDomains = null;
            if (_credentialMappingService is not null)
            {
                try
                {
                    credentialDomains = await _credentialMappingService.GetConfiguredDomainsAsync(ct);
                    _logger.LogInformation(
                        "Fallback provisioning resolved {Count} credential domain(s): [{Domains}]",
                        credentialDomains.Count,
                        string.Join(", ", credentialDomains));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve credential domains during fallback provisioning");
                }
            }

            context.ReportProgress("Creating sandbox...");
            sandboxId = await _client.CreateSandboxAsync(userId, conversationId, context.ReportProgress, ct, credentialDomains);
        }
        return sandboxId;
    }

    private static bool IsTransient(System.Net.HttpStatusCode? status) =>
        status is System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.GatewayTimeout or System.Net.HttpStatusCode.RequestTimeout;

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

        Skills are stored in two locations:
        - `/home/sandbox/skills/` — system skills (read-only)
        - `/home/sandbox/files/skills/` — user skills (read-write, persisted)

        To discover available skills:
        `sandbox_exec("ls /home/sandbox/skills/ /home/sandbox/files/skills/ 2>/dev/null")`

        Before using a skill, read its instructions:
        `sandbox_exec("cat /home/sandbox/skills/{name}/SKILL.md")`

        ### Pre-installed packages

        - Python: requests, numpy, pandas, python-dateutil, pypdf, pdfplumber, reportlab, pytesseract, pdf2image, openpyxl, python-pptx, markitdown[pptx], Pillow, imageio, imageio-ffmpeg
        - Node.js: typescript, ts-node, docx, pptxgenjs (global)
        - System: pandoc, poppler-utils, qpdf, tesseract-ocr, libreoffice
        """;
}
