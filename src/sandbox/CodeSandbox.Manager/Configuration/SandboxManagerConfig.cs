using System.ComponentModel.DataAnnotations;

namespace CodeSandbox.Manager.Configuration;

public class SandboxManagerConfig
{
    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid namespace name")]
    public string TargetNamespace { get; set; } = "sandbox-containers";

    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid runtime class name")]
    public string RuntimeClassName { get; set; } = "gvisor";

    [Required]
    public ResourceConfig DefaultResourceRequests { get; set; } = new();

    [Required]
    public ResourceConfig DefaultResourceLimits { get; set; } = new();

    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid pod name prefix")]
    public string PodNamePrefix { get; set; } = "sandbox";

    [Required]
    public string DefaultImage { get; set; } = "ghcr.io/andyjmorgan/donkeywork-agents/sandbox-executor:latest";

    [Range(30, 300, ErrorMessage = "Pod ready timeout must be between 30 and 300 seconds")]
    public int PodReadyTimeoutSeconds { get; set; } = 90;

    [Range(1, 1440, ErrorMessage = "Idle timeout must be between 1 and 1440 minutes")]
    public int IdleTimeoutMinutes { get; set; } = 60;

    [Range(1, 60, ErrorMessage = "Cleanup check interval must be between 1 and 60 minutes")]
    public int CleanupCheckIntervalMinutes { get; set; } = 1;

    [Range(1, 500, ErrorMessage = "Max total containers must be between 1 and 500")]
    public int MaxTotalContainers { get; set; } = 50;

    // Auth proxy sidecar settings
    public bool EnableAuthProxy { get; set; } = false;

    public string AuthProxyImage { get; set; } = "ghcr.io/andyjmorgan/donkeywork-agents/sandbox-authproxy:latest";

    public ResourceConfig AuthProxySidecarResourceRequests { get; set; } = new() { MemoryMi = 64, CpuMillicores = 100 };
    public ResourceConfig AuthProxySidecarResourceLimits { get; set; } = new() { MemoryMi = 128, CpuMillicores = 250 };

    [Range(1, 65535, ErrorMessage = "Auth proxy port must be between 1 and 65535")]
    public int AuthProxyPort { get; set; } = 8080;

    [Range(1, 65535, ErrorMessage = "Auth proxy health port must be between 1 and 65535")]
    public int AuthProxyHealthPort { get; set; } = 8081;

    public List<string> AuthProxyBlockedDomains { get; set; } = new();

    public string AuthProxyCaSecretName { get; set; } = "sandbox-proxy-ca";

    // Internal gRPC credential store settings
    public string? CredentialStoreGrpcUrl { get; set; }

    public string GrpcClientSecretName { get; set; } = "internal-grpc-client";

    public string GrpcCaSecretName { get; set; } = "internal-grpc-ca";

    // Persistent storage settings (single PVC with subPath mounts)
    public bool EnablePersistentStorage { get; set; } = true;

    public string StoragePvcName { get; set; } = "seaweedfs-buckets";

    public string UserFilesSubPathPrefix { get; set; } = "files";

    public string UserFilesMountPath { get; set; } = "/home/sandbox/files";

    public string SkillsSubPath { get; set; } = "skills";

    public string SkillsMountPath { get; set; } = "/home/sandbox/skills";

    // MCP Server Settings

    /// <summary>
    /// Docker image for MCP server pods.
    /// </summary>
    public string McpServerImage { get; set; } = "ghcr.io/andyjmorgan/donkeywork-agents/sandbox-executor:latest";

    /// <summary>
    /// Prefix for MCP server pod names (e.g. "mcp-a1b2c3d4").
    /// </summary>
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid MCP pod name prefix")]
    public string McpPodNamePrefix { get; set; } = "mcp";

    /// <summary>
    /// Idle timeout in minutes before MCP server pods are cleaned up.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "MCP idle timeout must be between 1 and 1440 minutes")]
    public int McpIdleTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum lifetime in minutes for MCP server pods regardless of activity.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "MCP max container lifetime must be between 1 and 1440 minutes")]
    public int McpMaxContainerLifetimeMinutes { get; set; } = 480;

    /// <summary>
    /// Default resource requests for MCP server containers (lighter than code sandboxes).
    /// </summary>
    public ResourceConfig McpDefaultResourceRequests { get; set; } = new() { MemoryMi = 128, CpuMillicores = 250 };

    /// <summary>
    /// Default resource limits for MCP server containers.
    /// </summary>
    public ResourceConfig McpDefaultResourceLimits { get; set; } = new() { MemoryMi = 256, CpuMillicores = 500 };

    // Leader election settings
    [Range(5, 60, ErrorMessage = "Lease duration must be between 5 and 60 seconds")]
    public int LeaderLeaseDurationSeconds { get; set; } = 15;

    // Optional: Direct k8s connection (alternative to kubeconfig)
    public KubernetesConnectionConfig? Connection { get; set; }
}

public class KubernetesConnectionConfig
{
    public string? ServerUrl { get; set; }
    public string? Token { get; set; }
    public bool SkipTlsVerify { get; set; } = false;
}

public class ResourceConfig
{
    [Range(1, 65536, ErrorMessage = "Memory must be between 1Mi and 65536Mi")]
    public int MemoryMi { get; set; }

    [Range(1, 64000, ErrorMessage = "CPU must be between 1m and 64000m")]
    public int CpuMillicores { get; set; }
}
