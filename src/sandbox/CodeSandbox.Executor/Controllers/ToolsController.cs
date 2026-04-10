using System.Text;
using CodeSandbox.Contracts.Events;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Contracts.Responses;
using CodeSandbox.Executor.Services;
using CodeSandbox.Executor.Tools;
using CodeSandbox.Executor.Tools.Editing;
using CodeSandbox.Executor.Tools.Reading;
using CodeSandbox.Executor.Tools.Search;
using CodeSandbox.Executor.Tools.Writing;
using Microsoft.AspNetCore.Mvc;

namespace CodeSandbox.Executor.Controllers;

[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private readonly ReadTool readTool;
    private readonly EditTool editTool;
    private readonly MultiEditTool multiEditTool;
    private readonly WriteTool writeTool;
    private readonly GlobTool globTool;
    private readonly GrepTool grepTool;
    private readonly ProcessTracker processTracker;
    private readonly ILogger<ToolsController> logger;

    public ToolsController(
        ReadTool readTool,
        EditTool editTool,
        MultiEditTool multiEditTool,
        WriteTool writeTool,
        GlobTool globTool,
        GrepTool grepTool,
        ProcessTracker processTracker,
        ILogger<ToolsController> logger)
    {
        this.readTool = readTool;
        this.editTool = editTool;
        this.multiEditTool = multiEditTool;
        this.writeTool = writeTool;
        this.globTool = globTool;
        this.grepTool = grepTool;
        this.processTracker = processTracker;
        this.logger = logger;
    }

    [HttpPost("bash")]
    public async Task<ToolResponse> Bash([FromBody] BashRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return new ToolResponse { Output = "Error: command must not be empty", IsError = true };
        }

        if (request.TimeoutSeconds <= 0)
        {
            return new ToolResponse { Output = "Error: timeoutSeconds must be greater than zero", IsError = true };
        }

        var process = new ManagedProcess(request.Command, this.processTracker, this.logger);
        var tracked = process.Start();

        return await this.StreamProcessOutput(tracked, request.TimeoutSeconds, ct);
    }

    [HttpPost("read")]
    public ToolResponse Read([FromBody] ReadRequest request) =>
        RunTool(() => this.readTool.Execute(request.FilePath, request.Offset, request.Limit));

    [HttpPost("edit")]
    public ToolResponse Edit([FromBody] EditRequest request) =>
        RunTool(() => this.editTool.Execute(request.FilePath, request.OldString, request.NewString, request.ReplaceAll));

    [HttpPost("write")]
    public ToolResponse Write([FromBody] WriteRequest request) =>
        RunTool(() => this.writeTool.Execute(request.FilePath, request.Content));

    [HttpPost("multiedit")]
    public ToolResponse MultiEdit([FromBody] MultiEditRequest request) =>
        RunTool(() => this.multiEditTool.Execute(request.FilePath, request.Edits));

    [HttpPost("glob")]
    public Task<ToolResponse> Glob([FromBody] GlobRequest request, CancellationToken ct) =>
        RunToolAsync(() => this.globTool.RunAsync(request.Pattern, request.Path, ct));

    [HttpPost("grep")]
    public Task<ToolResponse> Grep([FromBody] GrepRequest request, CancellationToken ct) =>
        RunToolAsync(() => this.grepTool.RunAsync(request.Pattern, request.Path, request.Include, ct));

    [HttpPost("resume")]
    public async Task<ToolResponse> Resume([FromBody] ResumeRequest request, CancellationToken ct)
    {
        var tracked = this.processTracker.TryGet(request.Pid);
        if (tracked == null)
        {
            return new ToolResponse
            {
                Output = $"Error: No tracked process found with PID {request.Pid}",
                IsError = true,
            };
        }

        return await this.StreamProcessOutput(tracked, request.TimeoutSeconds, ct);
    }

    private static ToolResponse RunTool(Func<ToolResult> action)
    {
        try
        {
            var result = action();
            return new ToolResponse { Output = result.Output };
        }
        catch (Exception ex)
        {
            return new ToolResponse { Output = $"Error: {ex.Message}", IsError = true };
        }
    }

    private static async Task<ToolResponse> RunToolAsync(Func<Task<ToolResult>> action)
    {
        try
        {
            var result = await action();
            return new ToolResponse { Output = result.Output };
        }
        catch (Exception ex)
        {
            return new ToolResponse { Output = $"Error: {ex.Message}", IsError = true };
        }
    }

    private async Task<ToolResponse> StreamProcessOutput(TrackedProcess tracked, int timeoutSeconds, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var output = new StringBuilder();
        bool timedOut = false;

        try
        {
            await foreach (var evt in tracked.ReconnectAsync(timeoutCts.Token))
            {
                if (evt is OutputEvent outputEvt)
                {
                    output.AppendLine(outputEvt.Data);
                }
                else if (evt is CompletedEvent completedEvt)
                {
                    this.processTracker.Remove(tracked.Pid);

                    var exitInfo = completedEvt.ExitCode != 0
                        ? $"\n[Exit code: {completedEvt.ExitCode}]"
                        : string.Empty;

                    return new ToolResponse { Output = output.ToString().TrimEnd() + exitInfo };
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            timedOut = true;
        }

        if (timedOut)
        {
            return new ToolResponse
            {
                Output = $"""
                    {output.ToString().TrimEnd()}

                    [Operation timed out after {timeoutSeconds}s. Process PID: {tracked.Pid}]
                    [Use the 'resume' tool with pid={tracked.Pid} to reconnect and retrieve remaining output]
                    """,
            };
        }

        return new ToolResponse { Output = output.ToString().TrimEnd() };
    }
}
