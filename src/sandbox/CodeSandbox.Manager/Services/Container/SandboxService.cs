using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CodeSandbox.Manager.Configuration;
using CodeSandbox.Manager.Models;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace CodeSandbox.Manager.Services.Container;

public class SandboxService : ISandboxService
{
    private readonly IKubernetes _client;
    private readonly SandboxManagerConfig _config;
    private readonly ILogger<SandboxService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SandboxService(
        IKubernetes client,
        IOptions<SandboxManagerConfig> config,
        ILogger<SandboxService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async IAsyncEnumerable<ContainerCreationEvent> CreateSandboxAsync(
        CreateSandboxRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = System.Threading.Channels.Channel.CreateUnbounded<ContainerCreationEvent>();

        // Start the creation process in the background
        var creationTask = CreateSandboxInternalAsync(request, channel.Writer, cancellationToken);

        // Stream events to caller as they arrive
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        // Ensure creation completes
        await creationTask;
    }

    private async Task CreateSandboxInternalAsync(
        CreateSandboxRequest request,
        System.Threading.Channels.ChannelWriter<ContainerCreationEvent> writer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating sandbox for user {UserId}, conversation {ConversationId}",
            request.UserId, request.ConversationId);

        // Check container limit before creating
        var currentCount = await GetTotalContainerCountAsync(cancellationToken);
        if (currentCount >= _config.MaxTotalContainers)
        {
            _logger.LogWarning("Container limit reached: {Current}/{Max}",
                currentCount, _config.MaxTotalContainers);
            writer.TryWrite(new ContainerFailedEvent
            {
                PodName = "N/A",
                Reason = $"Maximum container limit of {_config.MaxTotalContainers} reached. Current: {currentCount}",
                ContainerInfo = null
            });
            writer.Complete();
            return;
        }

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var podName = $"{_config.PodNamePrefix}-{uniqueId}";
        var startTime = DateTime.UtcNow;

        var pod = BuildPodSpec(podName, request);

        V1Pod? createdPod = null;
        try
        {
            createdPod = await _client.CoreV1.CreateNamespacedPodAsync(
                pod,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully created sandbox: {PodName}", podName);

            // Emit created event
            writer.TryWrite(new ContainerCreatedEvent
            {
                PodName = podName,
                Phase = createdPod.Status?.Phase ?? "Pending"
            });

            // Always wait for ready
            var timeout = TimeSpan.FromSeconds(_config.PodReadyTimeoutSeconds);
            var deadline = DateTime.UtcNow.Add(timeout);
            var pollInterval = TimeSpan.FromSeconds(2);
            var attemptNumber = 0;

            while (DateTime.UtcNow < deadline)
            {
                attemptNumber++;

                try
                {
                    var currentPod = await _client.CoreV1.ReadNamespacedPodAsync(
                        podName,
                        _config.TargetNamespace,
                        cancellationToken: cancellationToken);

                    // Check if pod is ready
                    if (IsPodReady(currentPod))
                    {
                        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        _logger.LogInformation("Pod {PodName} is ready after {AttemptNumber} attempts", podName, attemptNumber);

                        writer.TryWrite(new ContainerReadyEvent
                        {
                            PodName = podName,
                            ContainerInfo = MapPodToContainerInfo(currentPod),
                            ElapsedSeconds = elapsed
                        });

                        return;
                    }

                    // Check if pod has failed
                    if (currentPod.Status?.Phase == "Failed")
                    {
                        _logger.LogWarning("Pod {PodName} failed during startup", podName);

                        writer.TryWrite(new ContainerFailedEvent
                        {
                            PodName = podName,
                            Reason = "Pod failed during startup",
                            ContainerInfo = MapPodToContainerInfo(currentPod)
                        });

                        return;
                    }

                    // Get detailed container status
                    var containerStatus = currentPod.Status?.ContainerStatuses?.FirstOrDefault();
                    string detailedMessage = $"Waiting for pod to be ready (attempt {attemptNumber})";

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

                    // Emit waiting event with detailed status
                    writer.TryWrite(new ContainerWaitingEvent
                    {
                        PodName = podName,
                        AttemptNumber = attemptNumber,
                        Phase = currentPod.Status?.Phase ?? "Unknown",
                        Message = detailedMessage
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking pod {PodName} status on attempt {AttemptNumber}", podName, attemptNumber);

                    writer.TryWrite(new ContainerWaitingEvent
                    {
                        PodName = podName,
                        AttemptNumber = attemptNumber,
                        Phase = "Unknown",
                        Message = $"Error checking status: {ex.Message}"
                    });
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            // Timeout - fetch final state and emit failed event
            _logger.LogWarning("Timeout waiting for pod {PodName} to be ready after {TimeoutSeconds}s",
                podName, _config.PodReadyTimeoutSeconds);

            try
            {
                var finalPod = await _client.CoreV1.ReadNamespacedPodAsync(
                    podName,
                    _config.TargetNamespace,
                    cancellationToken: cancellationToken);

                writer.TryWrite(new ContainerFailedEvent
                {
                    PodName = podName,
                    Reason = $"Timeout after {_config.PodReadyTimeoutSeconds}s",
                    ContainerInfo = MapPodToContainerInfo(finalPod)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch final pod state after timeout");
                writer.TryWrite(new ContainerFailedEvent
                {
                    PodName = podName,
                    Reason = $"Timeout after {_config.PodReadyTimeoutSeconds}s (unable to fetch final state)",
                    ContainerInfo = null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sandbox with image: {Image}", _config.DefaultImage);

            writer.TryWrite(new ContainerFailedEvent
            {
                PodName = podName,
                Reason = $"Creation failed: {ex.Message}",
                ContainerInfo = createdPod != null ? MapPodToContainerInfo(createdPod) : null
            });
        }
        finally
        {
            writer.Complete();
        }
    }

    public async Task<List<SandboxInfo>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing all sandboxes in namespace: {Namespace}", _config.TargetNamespace);

        try
        {
            var podList = await _client.CoreV1.ListNamespacedPodAsync(
                _config.TargetNamespace,
                labelSelector: "managed-by=CodeSandbox-Manager,container-type=sandbox",
                cancellationToken: cancellationToken);

            var sandboxes = podList.Items
                .Select(MapPodToContainerInfo)
                .ToList();

            _logger.LogInformation("Found {Count} sandboxes", sandboxes.Count);

            return sandboxes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list sandboxes");
            throw;
        }
    }

    public async Task<SandboxInfo?> GetContainerAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting sandbox: {PodName}", podName);

        try
        {
            var pod = await _client.CoreV1.ReadNamespacedPodAsync(
                podName,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            if (pod.Spec.RuntimeClassName != _config.RuntimeClassName)
            {
                _logger.LogWarning("Pod {PodName} is not a sandbox container", podName);
                return null;
            }

            return MapPodToContainerInfo(pod);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Sandbox not found: {PodName}", podName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sandbox: {PodName}", podName);
            throw;
        }
    }

    public async Task<DeleteSandboxResponse> DeleteContainerAsync(
        string podName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting sandbox: {PodName}", podName);

        try
        {
            await _client.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _config.TargetNamespace,
                body: new V1DeleteOptions { GracePeriodSeconds = 30 },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully deleted sandbox: {PodName}", podName);

            return new DeleteSandboxResponse
            {
                Success = true,
                Message = $"Sandbox {podName} deleted successfully",
                PodName = podName
            };
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Sandbox not found for deletion: {PodName}", podName);
            return new DeleteSandboxResponse
            {
                Success = false,
                Message = $"Sandbox {podName} not found",
                PodName = podName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sandbox: {PodName}", podName);
            return new DeleteSandboxResponse
            {
                Success = false,
                Message = $"Failed to delete sandbox: {ex.Message}",
                PodName = podName
            };
        }
    }

    public async Task<DeleteAllSandboxesResponse> DeleteAllContainersAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting all sandboxes in namespace: {Namespace}", _config.TargetNamespace);

        var response = new DeleteAllSandboxesResponse();

        try
        {
            var containers = await ListContainersAsync(cancellationToken);

            foreach (var container in containers)
            {
                try
                {
                    await _client.CoreV1.DeleteNamespacedPodAsync(
                        container.Name,
                        _config.TargetNamespace,
                        body: new V1DeleteOptions { GracePeriodSeconds = 0 },
                        cancellationToken: cancellationToken);

                    response.DeletedPods.Add(container.Name);
                    response.DeletedCount++;
                    _logger.LogInformation("Deleted sandbox: {PodName}", container.Name);
                }
                catch (Exception ex)
                {
                    response.FailedPods.Add(container.Name);
                    response.FailedCount++;
                    _logger.LogWarning(ex, "Failed to delete sandbox: {PodName}", container.Name);
                }
            }

            _logger.LogInformation("Deleted {DeletedCount} sandboxes, {FailedCount} failed",
                response.DeletedCount, response.FailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all sandboxes");
            throw;
        }

        return response;
    }

    public async Task<SandboxInfo?> FindSandboxAsync(
        string userId,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding sandbox for user {UserId}, conversation {ConversationId}", userId, conversationId);

        try
        {
            var labelSelector = $"managed-by=CodeSandbox-Manager,container-type=sandbox,user-id={userId},conversation-id={conversationId}";
            var podList = await _client.CoreV1.ListNamespacedPodAsync(
                _config.TargetNamespace,
                labelSelector: labelSelector,
                cancellationToken: cancellationToken);

            if (podList.Items.Count == 0)
                return null;

            // Prefer a running/ready pod, otherwise return the first one
            var readyPod = podList.Items.FirstOrDefault(IsPodReady);
            var pod = readyPod ?? podList.Items[0];

            return MapPodToContainerInfo(pod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find sandbox for user {UserId}, conversation {ConversationId}", userId, conversationId);
            throw;
        }
    }

    private async Task<int> GetTotalContainerCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            var allPods = await _client.CoreV1.ListNamespacedPodAsync(
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            return allPods.Items
                .Count(p => p.Spec.RuntimeClassName == _config.RuntimeClassName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total container count");
            return 0;
        }
    }

    private V1Pod BuildPodSpec(string podName, CreateSandboxRequest request)
    {
        var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var labels = new Dictionary<string, string>
        {
            ["app"] = "sandbox-manager",
            ["managed-by"] = "CodeSandbox-Manager",
            ["container-type"] = "sandbox",
            ["user-id"] = request.UserId,
            ["conversation-id"] = request.ConversationId
        };

        // Annotations for timestamps (source of truth)
        var annotations = new Dictionary<string, string>
        {
            [AnnotationHelper.CreatedAtAnnotation] = nowTimestamp,
            [AnnotationHelper.LastActivityAnnotation] = nowTimestamp
        };

        var container = new V1Container
        {
            Name = "workload",
            Image = _config.DefaultImage,
            ImagePullPolicy = "Always",
            Resources = BuildResourceRequirements(request.Resources),
            Stdin = true,
            Tty = true
        };

        // Build environment variables: proxy env vars first, then user vars (user overrides)
        var envVars = new List<V1EnvVar>();

        if (_config.EnableAuthProxy)
        {
            var proxyUrl = $"http://127.0.0.1:{_config.AuthProxyPort}";
            envVars.AddRange(new[]
            {
                new V1EnvVar { Name = "HTTP_PROXY", Value = proxyUrl },
                new V1EnvVar { Name = "HTTPS_PROXY", Value = proxyUrl },
                new V1EnvVar { Name = "http_proxy", Value = proxyUrl },
                new V1EnvVar { Name = "https_proxy", Value = proxyUrl },
                new V1EnvVar { Name = "NO_PROXY", Value = "localhost,127.0.0.1" },
                new V1EnvVar { Name = "no_proxy", Value = "localhost,127.0.0.1" },
                new V1EnvVar { Name = "NODE_EXTRA_CA_CERTS", Value = "/etc/proxy-ca/ca.crt" },
                new V1EnvVar { Name = "GIT_SSL_CAINFO", Value = "/etc/proxy-ca/ca.crt" },
            });
        }

        if (request.EnvironmentVariables != null && request.EnvironmentVariables.Count > 0)
        {
            envVars.AddRange(request.EnvironmentVariables
                .Select(kvp => new V1EnvVar { Name = kvp.Key, Value = kvp.Value }));
        }

        if (envVars.Count > 0)
        {
            container.Env = envVars;
        }

        var containers = new List<V1Container> { container };
        var volumes = new List<V1Volume>();
        var workloadMounts = new List<V1VolumeMount>();

        // Persistent storage: single PVC with subPath mounts
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

            // Per-user files (read-write)
            var userSubPath = string.IsNullOrEmpty(_config.UserFilesSubPathPrefix)
                ? $"{request.UserId}/files"
                : $"{_config.UserFilesSubPathPrefix}/{request.UserId}/files";

            workloadMounts.Add(new V1VolumeMount
            {
                Name = "buckets",
                MountPath = _config.UserFilesMountPath,
                SubPath = userSubPath
            });

            // Shared skills (read-only)
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

        if (_config.EnableAuthProxy)
        {
            // Mount CA public cert into workload container
            workloadMounts.Add(new V1VolumeMount
            {
                Name = "proxy-ca-public",
                MountPath = "/etc/proxy-ca",
                ReadOnlyProperty = true
            });

            // Add sidecar container
            containers.Add(BuildAuthProxySidecar());

            // Add volumes for CA cert
            volumes.Add(new V1Volume
            {
                Name = "proxy-ca-public",
                Secret = new V1SecretVolumeSource
                {
                    SecretName = _config.AuthProxyCaSecretName,
                    Items = new List<V1KeyToPath>
                    {
                        new() { Key = "tls.crt", Path = "ca.crt" }
                    }
                }
            });

            volumes.Add(new V1Volume
            {
                Name = "proxy-ca-full",
                Secret = new V1SecretVolumeSource
                {
                    SecretName = _config.AuthProxyCaSecretName,
                    Items = new List<V1KeyToPath>
                    {
                        new() { Key = "tls.crt", Path = "ca.crt" },
                        new() { Key = "tls.key", Path = "ca.key" }
                    }
                }
            });
        }

        if (workloadMounts.Count > 0)
        {
            container.VolumeMounts = workloadMounts;
        }

        var pod = new V1Pod
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
                Containers = containers,
                Volumes = volumes.Count > 0 ? volumes : null
            }
        };

        return pod;
    }

    private V1Container BuildAuthProxySidecar()
    {
        var envVars = new List<V1EnvVar>
        {
            new() { Name = "ProxyConfiguration__ProxyPort", Value = _config.AuthProxyPort.ToString() },
            new() { Name = "ProxyConfiguration__HealthPort", Value = _config.AuthProxyHealthPort.ToString() },
            new() { Name = "ProxyConfiguration__CaCertificatePath", Value = "/certs/ca.crt" },
            new() { Name = "ProxyConfiguration__CaPrivateKeyPath", Value = "/certs/ca.key" },
        };

        // Add blocked domains as indexed environment variables
        for (int i = 0; i < _config.AuthProxyBlockedDomains.Count; i++)
        {
            envVars.Add(new V1EnvVar
            {
                Name = $"ProxyConfiguration__BlockedDomains__{i}",
                Value = _config.AuthProxyBlockedDomains[i]
            });
        }

        // Add domain credentials as indexed environment variables
        for (int i = 0; i < _config.AuthProxyDomainCredentials.Count; i++)
        {
            var cred = _config.AuthProxyDomainCredentials[i];
            envVars.Add(new V1EnvVar
            {
                Name = $"ProxyConfiguration__DomainCredentials__{i}__BaseDomain",
                Value = cred.BaseDomain
            });

            foreach (var (headerName, headerValue) in cred.Headers)
            {
                envVars.Add(new V1EnvVar
                {
                    Name = $"ProxyConfiguration__DomainCredentials__{i}__Headers__{headerName}",
                    Value = headerValue
                });
            }
        }

        return new V1Container
        {
            Name = "auth-proxy",
            Image = _config.AuthProxyImage,
            ImagePullPolicy = "Always",
            Ports = new List<V1ContainerPort>
            {
                new() { ContainerPort = _config.AuthProxyPort },
                new() { ContainerPort = _config.AuthProxyHealthPort }
            },
            Env = envVars,
            VolumeMounts = new List<V1VolumeMount>
            {
                new()
                {
                    Name = "proxy-ca-full",
                    MountPath = "/certs",
                    ReadOnlyProperty = true
                }
            },
            Resources = new V1ResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    ["memory"] = new ResourceQuantity($"{_config.AuthProxySidecarResourceRequests.MemoryMi}Mi"),
                    ["cpu"] = new ResourceQuantity($"{_config.AuthProxySidecarResourceRequests.CpuMillicores}m")
                },
                Limits = new Dictionary<string, ResourceQuantity>
                {
                    ["memory"] = new ResourceQuantity($"{_config.AuthProxySidecarResourceLimits.MemoryMi}Mi"),
                    ["cpu"] = new ResourceQuantity($"{_config.AuthProxySidecarResourceLimits.CpuMillicores}m")
                }
            },
            ReadinessProbe = new V1Probe
            {
                HttpGet = new V1HTTPGetAction
                {
                    Path = "/healthz",
                    Port = _config.AuthProxyHealthPort
                },
                InitialDelaySeconds = 2,
                PeriodSeconds = 5
            }
        };
    }

    private V1ResourceRequirements BuildResourceRequirements(ResourceRequirements? resources)
    {
        var requests = new Dictionary<string, ResourceQuantity>();
        var limits = new Dictionary<string, ResourceQuantity>();

        if (resources?.Requests != null)
        {
            if (resources.Requests.MemoryMi.HasValue)
                requests["memory"] = new ResourceQuantity($"{resources.Requests.MemoryMi}Mi");
            if (resources.Requests.CpuMillicores.HasValue)
                requests["cpu"] = new ResourceQuantity($"{resources.Requests.CpuMillicores}m");
        }
        else
        {
            requests["memory"] = new ResourceQuantity($"{_config.DefaultResourceRequests.MemoryMi}Mi");
            requests["cpu"] = new ResourceQuantity($"{_config.DefaultResourceRequests.CpuMillicores}m");
        }

        if (resources?.Limits != null)
        {
            if (resources.Limits.MemoryMi.HasValue)
                limits["memory"] = new ResourceQuantity($"{resources.Limits.MemoryMi}Mi");
            if (resources.Limits.CpuMillicores.HasValue)
                limits["cpu"] = new ResourceQuantity($"{resources.Limits.CpuMillicores}m");
        }
        else
        {
            limits["memory"] = new ResourceQuantity($"{_config.DefaultResourceLimits.MemoryMi}Mi");
            limits["cpu"] = new ResourceQuantity($"{_config.DefaultResourceLimits.CpuMillicores}m");
        }

        return new V1ResourceRequirements
        {
            Requests = requests,
            Limits = limits
        };
    }

    private SandboxInfo MapPodToContainerInfo(V1Pod pod)
    {
        var readyCondition = pod.Status?.Conditions?
            .FirstOrDefault(c => c.Type == "Ready");

        var containerImage = pod.Spec?.Containers?.FirstOrDefault()?.Image;

        var lastActivity = AnnotationHelper.ParseTimestampAnnotation(
            pod.Metadata.Annotations,
            AnnotationHelper.LastActivityAnnotation);

        return new SandboxInfo
        {
            Name = pod.Metadata.Name,
            Phase = pod.Status?.Phase ?? "Unknown",
            IsReady = readyCondition?.Status == "True",
            CreatedAt = pod.Metadata.CreationTimestamp,
            NodeName = pod.Spec?.NodeName,
            PodIP = pod.Status?.PodIP,
            Labels = pod.Metadata.Labels != null ? new Dictionary<string, string>(pod.Metadata.Labels) : null,
            Image = containerImage,
            LastActivity = lastActivity
        };
    }

    private bool IsPodReady(V1Pod pod)
    {
        if (pod.Status?.Phase != "Running")
            return false;

        var readyCondition = pod.Status.Conditions?
            .FirstOrDefault(c => c.Type == "Ready");

        return readyCondition?.Status == "True";
    }

    // Execution passthrough implementation
    public async Task ExecuteCommandAsync(
        string sandboxId,
        ExecutionRequest request,
        Stream responseStream,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing command in sandbox {SandboxId}: {Command}", sandboxId, request.Command);

        // Update last activity time (fire and forget)
        _ = UpdateLastActivityAsync(sandboxId, cancellationToken);

        // Get pod IP
        var podIp = await GetPodIpAsync(sandboxId, cancellationToken);

        // Create HTTP request to CodeExecution API
        var httpClient = _httpClientFactory.CreateClient();
        var apiUrl = $"http://{podIp}:8666/api/execute";

        var jsonContent = JsonSerializer.Serialize(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to sandbox {SandboxId} CodeExecution API", sandboxId);
            throw;
        }

        // Stream the response directly through without parsing
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await stream.CopyToAsync(responseStream, cancellationToken);

        _logger.LogInformation("Command execution stream completed for sandbox {SandboxId}", sandboxId);
    }

    public async Task<string> GetPodIpAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pod = await _client.CoreV1.ReadNamespacedPodAsync(
                sandboxId,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(pod.Status?.PodIP))
            {
                throw new InvalidOperationException($"Pod {sandboxId} does not have an IP address yet");
            }

            return pod.Status.PodIP;
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Sandbox {sandboxId} not found", ex);
        }
    }

    public async Task UpdateLastActivityAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        try
        {
            var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var patch = new V1Patch(
                $"[{{\"op\": \"replace\", \"path\": \"/metadata/annotations/{AnnotationHelper.LastActivityAnnotation.Replace("/", "~1")}\", \"value\": \"{nowTimestamp}\"}}]",
                V1Patch.PatchType.JsonPatch);

            await _client.CoreV1.PatchNamespacedPodAsync(
                patch,
                sandboxId,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Updated last activity annotation for sandbox {SandboxId}", sandboxId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update last activity for sandbox {SandboxId}", sandboxId);
        }
    }

    public async Task<DateTime?> GetLastActivityAsync(string sandboxId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pod = await _client.CoreV1.ReadNamespacedPodAsync(
                sandboxId,
                _config.TargetNamespace,
                cancellationToken: cancellationToken);

            return AnnotationHelper.ParseTimestampAnnotation(
                pod.Metadata.Annotations,
                AnnotationHelper.LastActivityAnnotation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get last activity for sandbox {SandboxId}", sandboxId);
            return null;
        }
    }
}

/// <summary>
/// Static helpers for parsing annotation timestamps.
/// </summary>
public static class AnnotationHelper
{
    public const string CreatedAtAnnotation = "codesandbox.donkeywork.dev/created-at";
    public const string LastActivityAnnotation = "codesandbox.donkeywork.dev/last-activity";

    public static DateTime? ParseTimestampAnnotation(IDictionary<string, string>? annotations, string key)
    {
        if (annotations?.TryGetValue(key, out var timestampStr) == true
            && long.TryParse(timestampStr, out var unixTimestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }
        return null;
    }
}
