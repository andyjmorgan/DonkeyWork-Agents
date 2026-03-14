using System.Net;
using System.Text.Json;
using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Models;
using CodeSandbox.Manager.Services.Container;
using Grpc.Core;
using Grpc.Net.Client;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Options;
using GrpcMcpServer = CodeSandbox.Contracts.Grpc.McpServer;

namespace CodeSandbox.Manager.Services.Mcp;

public class McpContainerService : IMcpContainerService
{
    private readonly IKubernetes _client;
    private readonly SandboxManagerConfig _config;
    private readonly ILogger<McpContainerService> _logger;

    public McpContainerService(
        IKubernetes client,
        IOptions<SandboxManagerConfig> config,
        ILogger<McpContainerService> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
    }

    #region Pod Lifecycle

    public async IAsyncEnumerable<ContainerCreationEvent> CreateMcpServerAsync(
        CreateMcpServerRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<ContainerCreationEvent>();

        var creationTask = CreateMcpServerInternalAsync(request, channel.Writer, cancellationToken);

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        await creationTask;
    }

    private async Task CreateMcpServerInternalAsync(
        CreateMcpServerRequest request,
        System.Threading.Channels.ChannelWriter<ContainerCreationEvent> writer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating MCP server container with image: {Image}", _config.McpServerImage);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var podName = $"{_config.McpPodNamePrefix}-{uniqueId}";
        var startTime = DateTime.UtcNow;

        var pod = BuildMcpPodSpec(podName, request);

        try
        {
            var createdPod = await _client.CoreV1.CreateNamespacedPodAsync(
                pod,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully created MCP container: {PodName}", podName);

            writer.TryWrite(new ContainerCreatedEvent
            {
                PodName = podName,
                Phase = createdPod.Status?.Phase ?? "Pending"
            });

            // Watch for pod readiness using Kubernetes watch API
            V1Pod? readyPod = null;
            var eventNumber = 0;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.PodReadyTimeoutSeconds));

            try
            {
                var listTask = _client.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
                    _config.TargetNamespace,
                    fieldSelector: $"metadata.name={podName}",
                    watch: true,
                    cancellationToken: timeoutCts.Token);

                #pragma warning disable CS0618 // WatchAsync is the best available API in KubernetesClient v18
                await foreach (var (eventType, watchPod) in listTask.WatchAsync<V1Pod, V1PodList>(
                    cancellationToken: timeoutCts.Token))
                #pragma warning restore CS0618
                {
                    eventNumber++;

                    if (eventType == WatchEventType.Deleted)
                    {
                        writer.TryWrite(new ContainerFailedEvent
                        {
                            PodName = podName,
                            Reason = "Pod was deleted during startup",
                            ContainerInfo = MapPodToSandboxInfo(watchPod)
                        });
                        return;
                    }

                    if (eventType == WatchEventType.Error)
                    {
                        writer.TryWrite(new ContainerWaitingEvent
                        {
                            PodName = podName,
                            AttemptNumber = eventNumber,
                            Phase = watchPod.Status?.Phase ?? "Unknown",
                            Message = "Watch received error event"
                        });
                        continue;
                    }

                    // ADDED or MODIFIED
                    if (IsPodReady(watchPod))
                    {
                        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        _logger.LogInformation("MCP pod {PodName} is ready after {EventCount} watch events", podName, eventNumber);

                        writer.TryWrite(new ContainerReadyEvent
                        {
                            PodName = podName,
                            ContainerInfo = MapPodToSandboxInfo(watchPod),
                            ElapsedSeconds = elapsed
                        });
                        readyPod = watchPod;
                        break;
                    }

                    if (watchPod.Status?.Phase == "Failed")
                    {
                        writer.TryWrite(new ContainerFailedEvent
                        {
                            PodName = podName,
                            Reason = "Pod failed during startup",
                            ContainerInfo = MapPodToSandboxInfo(watchPod)
                        });
                        return;
                    }

                    // Emit waiting event with detailed status
                    var containerStatus = watchPod.Status?.ContainerStatuses?.FirstOrDefault();
                    string detailedMessage = $"Waiting for pod to be ready (event {eventNumber})";

                    if (containerStatus?.State?.Waiting != null)
                    {
                        var waiting = containerStatus.State.Waiting;
                        var reason = waiting.Reason ?? "Unknown";
                        var message = waiting.Message ?? "";
                        detailedMessage = reason switch
                        {
                            "ContainerCreating" => "Creating container...",
                            "PodInitializing" => "Initializing pod...",
                            "ErrImagePull" => $"Error pulling image: {message}",
                            "ImagePullBackOff" => $"Failed to pull image, retrying: {message}",
                            _ => $"{reason}: {message}"
                        };
                    }
                    else if (containerStatus?.State?.Running != null)
                    {
                        detailedMessage = "Container running, waiting for readiness checks...";
                    }

                    writer.TryWrite(new ContainerWaitingEvent
                    {
                        PodName = podName,
                        AttemptNumber = eventNumber,
                        Phase = watchPod.Status?.Phase ?? "Unknown",
                        Message = detailedMessage
                    });
                }

                // If pod became ready and a command was provided, start the MCP process
                if (readyPod != null && !string.IsNullOrWhiteSpace(request.Command))
                {
                    var commandDisplay = $"{request.Command} {string.Join(" ", request.Arguments)}";
                    writer.TryWrite(new McpServerStartingEvent
                    {
                        PodName = podName,
                        Message = $"Starting MCP process: {commandDisplay}"
                    });

                    try
                    {
                        var podIp = readyPod.Status?.PodIP
                            ?? throw new InvalidOperationException("Pod has no IP");

                        await foreach (var startEvt in StartMcpProcessOnPodGrpcAsync(podIp, request, cancellationToken))
                        {
                            writer.TryWrite(new McpServerStartingEvent
                            {
                                PodName = podName,
                                Message = $"[{startEvt.EventType}] {startEvt.Message}"
                            });
                        }

                        var totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        writer.TryWrite(new McpServerStartedEvent
                        {
                            PodName = podName,
                            ServerInfo = MapPodToMcpServerInfo(readyPod, commandDisplay, Models.McpProcessStatus.Ready),
                            ElapsedSeconds = totalElapsed
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start MCP process in {PodName}", podName);
                        writer.TryWrite(new McpServerStartFailedEvent
                        {
                            PodName = podName,
                            Reason = $"MCP process failed to start: {ex.Message}"
                        });
                    }
                }

                return;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                _logger.LogWarning("Timeout waiting for MCP pod {PodName} to be ready after {TimeoutSeconds}s",
                    podName, _config.PodReadyTimeoutSeconds);

                writer.TryWrite(new ContainerFailedEvent
                {
                    PodName = podName,
                    Reason = $"Timeout after {_config.PodReadyTimeoutSeconds}s"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MCP container");
            writer.TryWrite(new ContainerFailedEvent
            {
                PodName = podName,
                Reason = $"Creation failed: {ex.Message}"
            });
        }
        finally
        {
            writer.Complete();
        }
    }

    public async Task<McpServerInfo?> FindMcpServerAsync(
        string userId,
        string mcpServerConfigId,
        CancellationToken cancellationToken = default)
    {
        var labelSelector = $"managed-by=CodeSandbox-Manager,container-type=mcp,user-id={userId},mcp-server-config-id={mcpServerConfigId}";

        var podList = await _client.CoreV1.ListNamespacedPodAsync(
            _config.TargetNamespace,
            labelSelector: labelSelector,
            cancellationToken: cancellationToken);

        if (podList.Items.Count == 0)
            return null;

        // Prefer a running/ready pod
        var readyPod = podList.Items.FirstOrDefault(IsPodReady);
        var pod = readyPod ?? podList.Items[0];

        return MapPodToMcpServerInfo(pod);
    }

    public async Task<McpServerInfo?> GetMcpServerAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pod = await _client.CoreV1.ReadNamespacedPodAsync(
                podName,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            return MapPodToMcpServerInfo(pod);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<McpServerInfo>> ListMcpServersAsync(CancellationToken cancellationToken = default)
    {
        var podList = await _client.CoreV1.ListNamespacedPodAsync(
            _config.TargetNamespace,
            labelSelector: "managed-by=CodeSandbox-Manager,container-type=mcp",
            cancellationToken: cancellationToken);

        return podList.Items
            .Where(p => p.Spec.RuntimeClassName == _config.RuntimeClassName)
            .Select(p => MapPodToMcpServerInfo(p))
            .ToList();
    }

    public async Task<DeleteSandboxResponse> DeleteMcpServerAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting MCP server: {PodName}", podName);

        try
        {
            // Try to stop MCP process gracefully first
            try
            {
                await StopMcpProcessAsync(podName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not stop MCP process before deletion (may already be stopped)");
            }

            await _client.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _config.TargetNamespace,
                body: new V1DeleteOptions { GracePeriodSeconds = 30 },
                cancellationToken: cancellationToken);

            return new DeleteSandboxResponse
            {
                Success = true,
                Message = $"MCP server {podName} deleted successfully",
                PodName = podName
            };
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeleteSandboxResponse
            {
                Success = false,
                Message = $"MCP server {podName} not found",
                PodName = podName
            };
        }
        catch (Exception ex)
        {
            return new DeleteSandboxResponse
            {
                Success = false,
                Message = $"Failed to delete MCP server: {ex.Message}",
                PodName = podName
            };
        }
    }

    #endregion

    #region MCP Process Management (gRPC to pod)

    public async IAsyncEnumerable<McpStartProcessEvent> StartMcpProcessAsync(
        string podName,
        McpStartRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var commandDisplay = $"{request.Command} {string.Join(" ", request.Arguments)}";
        _logger.LogInformation("Starting MCP process in {PodName}: {Command}", podName, commandDisplay);

        var podIp = await GetPodIpAsync(podName, cancellationToken);

        yield return new McpStartProcessEvent
        {
            EventType = "connecting",
            Message = $"Connecting to gRPC at {podIp}:8666"
        };

        // Store launch command in annotation
        try
        {
            var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var patch = new V1Patch(
                JsonSerializer.Serialize(new[]
                {
                    new { op = "add", path = $"/metadata/annotations/{AnnotationHelper.McpLaunchCommandAnnotation.Replace("/", "~1")}", value = commandDisplay },
                    new { op = "replace", path = $"/metadata/annotations/{AnnotationHelper.LastActivityAnnotation.Replace("/", "~1")}", value = nowTimestamp }
                }),
                V1Patch.PatchType.JsonPatch);

            await _client.CoreV1.PatchNamespacedPodAsync(
                patch, podName, _config.TargetNamespace,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store launch command annotation for {PodName}", podName);
        }

        await foreach (var evt in StartMcpProcessOnPodGrpcAsync(podIp, new CreateMcpServerRequest
        {
            UserId = "",
            McpServerConfigId = "",
            Command = request.Command,
            Arguments = request.Arguments,
            PreExecScripts = request.PreExecScripts,
            TimeoutSeconds = request.TimeoutSeconds,
            WorkingDirectory = request.WorkingDirectory
        }, cancellationToken))
        {
            yield return evt;
        }
    }

    public async Task<McpProxyResponse> ProxyMcpRequestAsync(
        string podName,
        string jsonRpcBody,
        int timeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        var podIp = await GetPodIpAsync(podName, cancellationToken);

        var bodyTruncated = jsonRpcBody.Length > 500 ? jsonRpcBody[..500] + "...(truncated)" : jsonRpcBody;
        _logger.LogInformation("ProxyMcpRequestAsync: pod={PodName} ip={PodIp}, sending {Length} chars: {Body}",
            podName, podIp, jsonRpcBody.Length, bodyTruncated);

        // Update last activity (fire-and-forget)
        _ = UpdateLastActivityAsync(podName, cancellationToken);

        using var channel = GrpcChannel.ForAddress($"http://{podIp}:8666");
        var client = new GrpcMcpServer.McpServerService.McpServerServiceClient(channel);

        var response = await client.ProxyRequestAsync(new GrpcMcpServer.JsonRpcRequest
        {
            Body = jsonRpcBody,
            TimeoutSeconds = timeoutSeconds
        }, cancellationToken: cancellationToken);

        _logger.LogInformation("ProxyMcpRequestAsync: pod={PodName} success, isNotification={IsNotification}",
            podName, response.IsNotification);

        return new McpProxyResponse
        {
            Body = response.Body,
            IsNotification = response.IsNotification
        };
    }

    public async Task<McpStatusResponse> GetMcpStatusAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        var podIp = await GetPodIpAsync(podName, cancellationToken);

        using var channel = GrpcChannel.ForAddress($"http://{podIp}:8666");
        var client = new GrpcMcpServer.McpServerService.McpServerServiceClient(channel);

        var response = await client.GetStatusAsync(new GrpcMcpServer.GetStatusRequest(), cancellationToken: cancellationToken);

        return new McpStatusResponse
        {
            State = response.State,
            Error = response.HasError ? response.Error : null,
            StartedAt = response.HasStartedAt ? DateTime.Parse(response.StartedAt) : null,
            LastRequestAt = response.HasLastRequestAt ? DateTime.Parse(response.LastRequestAt) : null
        };
    }

    public async Task StopMcpProcessAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        var podIp = await GetPodIpAsync(podName, cancellationToken);

        using var channel = GrpcChannel.ForAddress($"http://{podIp}:8666");
        var client = new GrpcMcpServer.McpServerService.McpServerServiceClient(channel);

        await client.StopAsync(new GrpcMcpServer.StopRequest(), cancellationToken: cancellationToken);
    }

    #endregion

    #region Private Helpers

    private V1Pod BuildMcpPodSpec(string podName, CreateMcpServerRequest request)
    {
        var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var labels = new Dictionary<string, string>
        {
            ["app"] = "sandbox-manager",
            ["managed-by"] = "CodeSandbox-Manager",
            ["container-type"] = "mcp",
            ["user-id"] = request.UserId,
            ["mcp-server-config-id"] = request.McpServerConfigId
        };

        var annotations = new Dictionary<string, string>
        {
            [AnnotationHelper.CreatedAtAnnotation] = nowTimestamp,
            [AnnotationHelper.LastActivityAnnotation] = nowTimestamp
        };

        if (!string.IsNullOrWhiteSpace(request.Command))
        {
            var commandDisplay = $"{request.Command} {string.Join(" ", request.Arguments)}";
            annotations[AnnotationHelper.McpLaunchCommandAnnotation] = commandDisplay;
        }

        var container = new V1Container
        {
            Name = "workload",
            Image = _config.McpServerImage,
            ImagePullPolicy = _config.ImagePullPolicy,
            Resources = new V1ResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    ["memory"] = new ResourceQuantity($"{_config.McpDefaultResourceRequests.MemoryMi}Mi"),
                    ["cpu"] = new ResourceQuantity($"{_config.McpDefaultResourceRequests.CpuMillicores}m")
                },
                Limits = new Dictionary<string, ResourceQuantity>
                {
                    ["memory"] = new ResourceQuantity($"{_config.McpDefaultResourceLimits.MemoryMi}Mi"),
                    ["cpu"] = new ResourceQuantity($"{_config.McpDefaultResourceLimits.CpuMillicores}m")
                }
            },
            Stdin = true,
            Tty = true
        };

        if (request.EnvironmentVariables != null && request.EnvironmentVariables.Count > 0)
        {
            container.Env = request.EnvironmentVariables
                .Select(kvp => new V1EnvVar { Name = kvp.Key, Value = kvp.Value })
                .ToList();
        }

        var volumes = new List<V1Volume>();
        var workloadMounts = new List<V1VolumeMount>();

        // Persistent storage: mount user files for MCP servers too
        if (_config.EnablePersistentStorage)
        {
            volumes.Add(new V1Volume
            {
                Name = "buckets",
                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                {
                    ClaimName = _config.StoragePvcName
                }
            });

            var userSubPath = string.IsNullOrEmpty(_config.UserFilesSubPathPrefix)
                ? $"{request.UserId}"
                : $"{_config.UserFilesSubPathPrefix}/{request.UserId}";

            workloadMounts.Add(new V1VolumeMount
            {
                Name = "buckets",
                MountPath = _config.UserFilesMountPath,
                SubPath = userSubPath
            });

            if (!string.IsNullOrEmpty(_config.SkillsSubPath))
            {
                workloadMounts.Add(new V1VolumeMount
                {
                    Name = "buckets",
                    MountPath = _config.SkillsMountPath,
                    SubPath = _config.SkillsSubPath,
                    ReadOnlyProperty = true
                });
            }
        }

        if (workloadMounts.Count > 0)
        {
            container.VolumeMounts = workloadMounts;
        }

        return new V1Pod
        {
            Metadata = new V1ObjectMeta
            {
                Name = podName,
                NamespaceProperty = _config.TargetNamespace,
                Labels = labels,
                Annotations = annotations
            },
            Spec = new V1PodSpec
            {
                RuntimeClassName = _config.RuntimeClassName,
                RestartPolicy = "Never",
                Containers = new List<V1Container> { container },
                Volumes = volumes.Count > 0 ? volumes : null
            }
        };
    }

    private async IAsyncEnumerable<McpStartProcessEvent> StartMcpProcessOnPodGrpcAsync(
        string podIp,
        CreateMcpServerRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var channel = GrpcChannel.ForAddress($"http://{podIp}:8666");
        var client = new GrpcMcpServer.McpServerService.McpServerServiceClient(channel);

        var grpcRequest = new GrpcMcpServer.McpStartRequest
        {
            Command = request.Command ?? "",
            TimeoutSeconds = request.TimeoutSeconds
        };

        foreach (var arg in request.Arguments)
            grpcRequest.Arguments.Add(arg);

        foreach (var script in request.PreExecScripts)
            grpcRequest.PreExecScripts.Add(script);

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
            grpcRequest.WorkingDirectory = request.WorkingDirectory;

        using var call = client.Start(grpcRequest, cancellationToken: cancellationToken);

        await foreach (var evt in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return new McpStartProcessEvent
            {
                EventType = evt.EventType,
                Message = evt.Message,
                Error = evt.HasError ? evt.Error : null
            };
        }
    }

    private async Task<string> GetPodIpAsync(string podName, CancellationToken cancellationToken)
    {
        var pod = await _client.CoreV1.ReadNamespacedPodAsync(
            podName, _config.TargetNamespace,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(pod.Status?.PodIP))
            throw new InvalidOperationException($"Pod {podName} does not have an IP address");

        return pod.Status.PodIP;
    }

    private async Task UpdateLastActivityAsync(string podName, CancellationToken cancellationToken)
    {
        try
        {
            var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var patch = new V1Patch(
                $"[{{\"op\": \"replace\", \"path\": \"/metadata/annotations/{AnnotationHelper.LastActivityAnnotation.Replace("/", "~1")}\", \"value\": \"{nowTimestamp}\"}}]",
                V1Patch.PatchType.JsonPatch);

            await _client.CoreV1.PatchNamespacedPodAsync(
                patch, podName, _config.TargetNamespace,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update last activity for MCP server {PodName}", podName);
        }
    }

    private bool IsPodReady(V1Pod pod)
    {
        if (pod.Status?.Phase != "Running") return false;
        var readyCondition = pod.Status.Conditions?.FirstOrDefault(c => c.Type == "Ready");
        return readyCondition?.Status == "True";
    }

    private SandboxInfo MapPodToSandboxInfo(V1Pod pod)
    {
        var readyCondition = pod.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready");
        var lastActivity = AnnotationHelper.ParseTimestampAnnotation(
            pod.Metadata.Annotations, AnnotationHelper.LastActivityAnnotation);

        return new SandboxInfo
        {
            Name = pod.Metadata.Name,
            Phase = pod.Status?.Phase ?? "Unknown",
            IsReady = readyCondition?.Status == "True",
            CreatedAt = pod.Metadata.CreationTimestamp,
            NodeName = pod.Spec?.NodeName,
            PodIP = pod.Status?.PodIP,
            Labels = pod.Metadata.Labels != null ? new Dictionary<string, string>(pod.Metadata.Labels) : null,
            Image = pod.Spec?.Containers?.FirstOrDefault()?.Image,
            LastActivity = lastActivity
        };
    }

    private McpServerInfo MapPodToMcpServerInfo(V1Pod pod, string? launchCommand = null, Models.McpProcessStatus? mcpStatus = null)
    {
        var readyCondition = pod.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready");
        var lastActivity = AnnotationHelper.ParseTimestampAnnotation(
            pod.Metadata.Annotations, AnnotationHelper.LastActivityAnnotation);

        var storedCommand = launchCommand;
        if (storedCommand == null)
        {
            pod.Metadata.Annotations?.TryGetValue(AnnotationHelper.McpLaunchCommandAnnotation, out storedCommand);
        }

        return new McpServerInfo
        {
            Name = pod.Metadata.Name,
            Phase = pod.Status?.Phase ?? "Unknown",
            IsReady = readyCondition?.Status == "True",
            CreatedAt = pod.Metadata.CreationTimestamp,
            NodeName = pod.Spec?.NodeName,
            PodIP = pod.Status?.PodIP,
            Labels = pod.Metadata.Labels != null ? new Dictionary<string, string>(pod.Metadata.Labels) : null,
            Image = pod.Spec?.Containers?.FirstOrDefault()?.Image,
            LastActivity = lastActivity,
            LaunchCommand = storedCommand,
            McpStatus = mcpStatus ?? Models.McpProcessStatus.Unknown
        };
    }

    #endregion
}
