using System.Diagnostics;
using CodeSandbox.Contracts.Events;

namespace CodeSandbox.Executor.Services;

/// <summary>
/// Creates and monitors a process, routing all output through a TrackedProcess.
/// The process is always tracked from the start, enabling reconnect for any reason.
/// </summary>
public class ManagedProcess
{
    private readonly string _command;
    private readonly ProcessTracker _processTracker;
    private readonly ILogger? _logger;

    public ManagedProcess(string command, ProcessTracker processTracker, ILogger? logger = null)
    {
        _command = command;
        _processTracker = processTracker;
        _logger = logger;
    }

    /// <summary>
    /// Starts the process and returns a TrackedProcess that can be subscribed to.
    /// The process runs in the background; the TrackedProcess receives all output events
    /// and the final CompletedEvent when the process exits.
    /// </summary>
    public TrackedProcess Start()
    {
        // Use the user's home directory as the working directory, creating it if it doesn't exist
        var workingDirectory = Environment.GetEnvironmentVariable("HOME") ?? Path.GetTempPath();
        if (!Directory.Exists(workingDirectory))
        {
            Directory.CreateDirectory(workingDirectory);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{_command.Replace("\"", "\\\"")}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true,
        };

        process.Start();
        var pid = process.Id;

        var tracked = new TrackedProcess(pid, _command);
        tracked.SetProcess(process);
        _processTracker.Add(tracked);

        // Wire up output handlers that route to the tracked process
        DataReceivedEventHandler outputHandler = (sender, e) =>
        {
            if (e.Data != null)
            {
                tracked.WriteEvent(new OutputEvent
                {
                    Pid = pid,
                    Stream = OutputStreamType.Stdout,
                    Data = e.Data
                });
            }
        };

        DataReceivedEventHandler errorHandler = (sender, e) =>
        {
            if (e.Data != null)
            {
                tracked.WriteEvent(new OutputEvent
                {
                    Pid = pid,
                    Stream = OutputStreamType.Stderr,
                    Data = e.Data
                });
            }
        };

        process.OutputDataReceived += outputHandler;
        process.ErrorDataReceived += errorHandler;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Monitor the process in the background until it exits
        _ = Task.Run(() => MonitorAsync(process, pid, tracked, outputHandler, errorHandler));

        _logger?.LogInformation(
            "Process started and tracked. Pid: {Pid}, Command: {Command}",
            pid,
            _command.Length > 50 ? _command[..50] + "..." : _command);

        return tracked;
    }

    private async Task MonitorAsync(
        Process process,
        int pid,
        TrackedProcess tracked,
        DataReceivedEventHandler outputHandler,
        DataReceivedEventHandler errorHandler)
    {
        try
        {
            await process.WaitForExitAsync();

            // CRITICAL: WaitForExitAsync() only waits for process exit, not for event handlers to finish.
            await process.WaitForExitAsync();

            tracked.WriteEvent(new CompletedEvent
            {
                Pid = pid,
                ExitCode = process.ExitCode,
                TimedOut = false
            });

            _logger?.LogInformation(
                "Process exited. Pid: {Pid}, ExitCode: {ExitCode}",
                pid,
                process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error monitoring process. Pid: {Pid}", pid);

            tracked.WriteEvent(new CompletedEvent
            {
                Pid = pid,
                ExitCode = -1,
                TimedOut = false
            });
        }
        finally
        {
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;

            try { process.Dispose(); } catch { }
        }
    }
}
