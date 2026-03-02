using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Services.Container;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace CodeSandbox.Manager.Services.Background;

public class ContainerCleanupService : BackgroundService
{
    private readonly IKubernetes _client;
    private readonly SandboxManagerConfig _config;
    private readonly ILogger<ContainerCleanupService> _logger;

    public ContainerCleanupService(
        IKubernetes client,
        IOptions<SandboxManagerConfig> config,
        ILogger<ContainerCleanupService> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Container cleanup service started. Idle timeout: {IdleTimeoutMinutes}m, Check interval: {CheckIntervalMinutes}m",
            _config.IdleTimeoutMinutes, _config.CleanupCheckIntervalMinutes);

        var checkInterval = TimeSpan.FromMinutes(_config.CleanupCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(checkInterval, stoppingToken);

            try
            {
                await CleanupContainersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during container cleanup");
            }
        }

        _logger.LogInformation("Container cleanup service stopped");
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

        // Filter to only pods that have a user-id label (i.e., they've been assigned)
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

        // Check running pods for idle timeout
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

        // Delete idle containers
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
