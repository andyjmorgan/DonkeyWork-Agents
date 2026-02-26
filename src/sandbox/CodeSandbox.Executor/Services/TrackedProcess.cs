using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CodeSandbox.Contracts.Events;

namespace CodeSandbox.Executor.Services;

/// <summary>
/// Represents a tracked process whose output is buffered for reconnect.
/// All processes are tracked from the start, allowing reconnect after timeout or disconnect.
/// </summary>
public class TrackedProcess
{
    private readonly object _lock = new();
    private readonly List<ExecutionEvent> _bufferedEvents = [];
    private readonly List<ChannelWriter<ExecutionEvent>> _subscribers = [];
    private Process? _process;

    public int Pid { get; }
    public string Command { get; }
    public DateTime StartedAt { get; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public int? ExitCode { get; private set; }
    public bool IsCompleted { get; private set; }

    public int BufferedEventCount
    {
        get { lock (_lock) { return _bufferedEvents.Count; } }
    }

    public TrackedProcess(int pid, string command)
    {
        Pid = pid;
        Command = command;
    }

    /// <summary>
    /// Set the process reference so it can be killed via the API.
    /// </summary>
    public void SetProcess(Process process)
    {
        _process = process;
    }

    /// <summary>
    /// Write an event to the buffer and forward to all active subscribers.
    /// When a CompletedEvent is received, marks the process as completed and
    /// closes all subscriber channels.
    /// </summary>
    public void WriteEvent(ExecutionEvent evt)
    {
        lock (_lock)
        {
            _bufferedEvents.Add(evt);

            for (int i = _subscribers.Count - 1; i >= 0; i--)
            {
                if (!_subscribers[i].TryWrite(evt))
                {
                    _subscribers.RemoveAt(i);
                }
            }

            if (evt is CompletedEvent ce)
            {
                IsCompleted = true;
                ExitCode = ce.ExitCode;
                CompletedAt = DateTime.UtcNow;

                foreach (var sub in _subscribers)
                {
                    sub.TryComplete();
                }

                _subscribers.Clear();
            }
        }
    }

    /// <summary>
    /// Reconnect to this process's output stream.
    /// Replays all buffered events, then streams live events until the process completes.
    /// </summary>
    public async IAsyncEnumerable<ExecutionEvent> ReconnectAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ExecutionEvent>();

        lock (_lock)
        {
            // Replay all buffered events
            foreach (var evt in _bufferedEvents)
            {
                channel.Writer.TryWrite(evt);
            }

            if (IsCompleted)
            {
                // Process already done - just replay buffer
                channel.Writer.Complete();
            }
            else
            {
                // Subscribe for live events
                _subscribers.Add(channel.Writer);
            }
        }

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            // Clean up subscriber if we're cancelled or done
            lock (_lock)
            {
                _subscribers.Remove(channel.Writer);
            }
        }
    }

    /// <summary>
    /// Force kill the tracked process.
    /// </summary>
    public void Kill()
    {
        try
        {
            var p = _process;
            if (p != null && !p.HasExited)
            {
                p.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process may have already exited
        }
    }
}
