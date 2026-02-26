using System.Collections.Concurrent;

namespace CodeSandbox.Executor.Services;

/// <summary>
/// Singleton service that tracks processes which timed out but are still running.
/// Allows reconnecting to their output streams and force-killing them.
/// Automatically cleans up completed processes after an expiry period.
/// </summary>
public class ProcessTracker : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<int, TrackedProcess> _processes = new();
    private readonly ILogger<ProcessTracker> _logger;
    private Timer? _cleanupTimer;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CompletedProcessExpiry = TimeSpan.FromHours(1);

    public ProcessTracker(ILogger<ProcessTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a process for tracking.
    /// </summary>
    public void Add(TrackedProcess tracked)
    {
        _processes[tracked.Pid] = tracked;
        _logger.LogInformation(
            "Tracking process. Pid: {Pid}, Command: {Command}",
            tracked.Pid,
            tracked.Command.Length > 50 ? tracked.Command[..50] + "..." : tracked.Command);
    }

    /// <summary>
    /// Try to get a tracked process by PID.
    /// </summary>
    public TrackedProcess? TryGet(int pid)
    {
        _processes.TryGetValue(pid, out var tracked);
        return tracked;
    }

    /// <summary>
    /// Get all tracked processes.
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetAll()
    {
        return _processes.Values.ToList();
    }

    /// <summary>
    /// Remove a tracked process and force-kill it if still running.
    /// </summary>
    public bool TryRemoveAndKill(int pid)
    {
        if (_processes.TryRemove(pid, out var tracked))
        {
            tracked.Kill();
            _logger.LogInformation("Killed and removed tracked process. Pid: {Pid}", pid);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove a completed tracked process.
    /// </summary>
    public void Remove(int pid)
    {
        _processes.TryRemove(pid, out _);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer = new Timer(CleanupExpiredProcesses, null, CleanupInterval, CleanupInterval);
        _logger.LogInformation("ProcessTracker started with cleanup every {Interval}", CleanupInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer?.Change(Timeout.Infinite, 0);

        // Kill all remaining tracked processes on shutdown
        foreach (var kvp in _processes)
        {
            kvp.Value.Kill();
        }

        _processes.Clear();
        _logger.LogInformation("ProcessTracker stopped, all tracked processes killed");
        return Task.CompletedTask;
    }

    private void CleanupExpiredProcesses(object? state)
    {
        var now = DateTime.UtcNow;

        foreach (var kvp in _processes)
        {
            var tracked = kvp.Value;
            if (tracked.IsCompleted &&
                tracked.CompletedAt.HasValue &&
                now - tracked.CompletedAt.Value > CompletedProcessExpiry)
            {
                if (_processes.TryRemove(kvp.Key, out _))
                {
                    _logger.LogInformation(
                        "Cleaned up expired tracked process. Pid: {Pid}, CompletedAt: {CompletedAt}",
                        tracked.Pid,
                        tracked.CompletedAt);
                }
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
