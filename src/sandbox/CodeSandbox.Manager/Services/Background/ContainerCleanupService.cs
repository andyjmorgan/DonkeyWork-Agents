using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Services.Container;
using k8s;
using k8s.LeaderElection;
using k8s.LeaderElection.ResourceLock;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace CodeSandbox.Manager.Services.Background;

/// <summary>
/// Background service that uses Kubernetes leader election to ensure only one manager
/// performs container cleanup at a time.
/// </summary>
public class ContainerCleanupService : BackgroundService
{
    private readonly IKubernetes _client;
    private readonly SandboxManagerConfig _config;
    private readonly ILogger<ContainerCleanupService> _logger;
    private readonly string _identity;
    private CancellationTokenSource? _leaderLoopCts;

    public ContainerCleanupService(
        IKubernetes client,
        IOptions<SandboxManagerConfig> config,
        ILogger<ContainerCleanupService> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
        _identity = $"{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Container cleanup service starting with identity: {Identity}. Idle timeout: {IdleTimeoutMinutes}m, Check interval: {CheckIntervalMinutes}m",
            _identity, _config.IdleTimeoutMinutes, _config.CleanupCheckIntervalMinutes);

        try
        {
            var resourceLock = new LeaseLock(
                _client,
                _config.TargetNamespace,
                "container-cleanup-leader",
                _identity);

            _logger.LogInformation(
                "Configuring leader election (LeaseDuration={LeaseDuration}s, RetryPeriod={RetryPeriod}s, RenewDeadline={RenewDeadline}s)",
                _config.LeaderLeaseDurationSeconds,
                _config.LeaderLeaseDurationSeconds / 3,
                _config.LeaderLeaseDurationSeconds * 2 / 3);

            var electionConfig = new LeaderElectionConfig(resourceLock)
            {
                LeaseDuration = TimeSpan.FromSeconds(_config.LeaderLeaseDurationSeconds),
                RetryPeriod = TimeSpan.FromSeconds(_config.LeaderLeaseDurationSeconds / 3),
                RenewDeadline = TimeSpan.FromSeconds(_config.LeaderLeaseDurationSeconds * 2 / 3),
            };

            var leaderElector = new LeaderElector(electionConfig);

            leaderElector.OnStartedLeading += () =>
            {
                _logger.LogInformation("I am now the LEADER for container cleanup");
                _leaderLoopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                _ = Task.Run(() => CleanupLoop(_leaderLoopCts.Token), stoppingToken);
            };

            leaderElector.OnStoppedLeading += () =>
            {
                _logger.LogWarning("Lost leadership for container cleanup");
                _leaderLoopCts?.Cancel();
            };

            leaderElector.OnNewLeader += (newLeader) =>
            {
                if (newLeader != _identity)
                {
                    _logger.LogInformation("New cleanup leader elected: {Leader}", newLeader);
                }
            };

            _logger.LogInformation("Starting leader election...");
            await leaderElector.RunUntilLeadershipLostAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Container cleanup service stopping gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Container cleanup service encountered an error during leader election");
            throw;
        }
    }

    private async Task CleanupLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cleanup loop (running every {Interval}m)",
            _config.CleanupCheckIntervalMinutes);

        var checkInterval = TimeSpan.FromMinutes(_config.CleanupCheckIntervalMinutes);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(checkInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cleanup loop cancelled");
                break;
            }

            try
            {
                await CleanupContainersAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during container cleanup");
            }
        }

        _logger.LogInformation("Cleanup loop stopped");
    }

    private async Task CleanupContainersAsync(CancellationToken cancellationToken)
    {
        await CleanupContainersByTypeAsync("sandbox", _config.IdleTimeoutMinutes, cancellationToken);
        await CleanupContainersByTypeAsync("mcp", _config.McpIdleTimeoutMinutes, cancellationToken);
    }

    private async Task CleanupContainersByTypeAsync(string containerType, int idleTimeoutMinutes, CancellationToken cancellationToken)
    {
        var userPods = await _client.CoreV1.ListNamespacedPodAsync(
            _config.TargetNamespace,
            labelSelector: $"managed-by=CodeSandbox-Manager,container-type={containerType}",
            cancellationToken: cancellationToken);

        var assignedPods = userPods.Items
            .Where(p => p.Metadata.Labels?.ContainsKey("user-id") == true)
            .ToList();

        if (!assignedPods.Any())
        {
            _logger.LogDebug("No assigned {ContainerType} containers to check for cleanup", containerType);
            return;
        }

        // Separate completed/failed pods from running pods
        var completedPods = assignedPods
            .Where(p => p.Status.Phase is "Succeeded" or "Failed")
            .ToList();

        var runningPods = assignedPods
            .Where(p => p.Status.Phase is not ("Succeeded" or "Failed"))
            .ToList();

        // Force-delete completed/failed pods (containers already exited)
        foreach (var pod in completedPods)
        {
            var reason = pod.Status.Phase == "Failed" ? "failed" : "completed";
            await DeletePodAsync(pod.Metadata.Name, $"{containerType} {reason}", cancellationToken, forceDelete: true);
        }

        var now = DateTimeOffset.UtcNow;
        var idleThreshold = TimeSpan.FromMinutes(idleTimeoutMinutes);
        var idleContainers = new List<V1Pod>();

        foreach (var pod in runningPods)
        {
            var lastActivity = AnnotationHelper.ParseTimestampAnnotation(
                pod.Metadata.Annotations,
                AnnotationHelper.LastActivityAnnotation);

            if (lastActivity.HasValue)
            {
                var idleTime = now - lastActivity.Value;
                if (idleTime >= idleThreshold)
                {
                    idleContainers.Add(pod);
                }
            }
        }

        if (completedPods.Count > 0 || idleContainers.Count > 0)
        {
            _logger.LogInformation(
                "{ContainerType} cleanup: {Running} running, {Idle} idle, {Completed} completed/failed",
                containerType, runningPods.Count, idleContainers.Count, completedPods.Count);
        }

        foreach (var pod in idleContainers)
        {
            await DeletePodAsync(pod.Metadata.Name, $"{containerType} idle timeout", cancellationToken);
        }
    }

    private async Task DeletePodAsync(string podName, string reason, CancellationToken cancellationToken, bool forceDelete = false)
    {
        try
        {
            await _client.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _config.TargetNamespace,
                body: new V1DeleteOptions { GracePeriodSeconds = forceDelete ? 0 : 30 },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted container {PodName} ({Reason})", podName, reason);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Pod {PodName} not found, may have been already deleted", podName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete container {PodName}", podName);
        }
    }
}
