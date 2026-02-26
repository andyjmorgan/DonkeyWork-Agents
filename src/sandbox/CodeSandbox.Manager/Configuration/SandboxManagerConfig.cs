using System.ComponentModel.DataAnnotations;

namespace CodeSandbox.Manager.Configuration;

public class SandboxManagerConfig
{
    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid namespace name")]
    public string TargetNamespace { get; set; } = "sandbox-containers";

    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid runtime class name")]
    public string RuntimeClassName { get; set; } = "kata-qemu";

    [Required]
    public ResourceConfig DefaultResourceRequests { get; set; } = new();

    [Required]
    public ResourceConfig DefaultResourceLimits { get; set; } = new();

    [Required]
    [RegularExpression(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?$", ErrorMessage = "Invalid pod name prefix")]
    public string PodNamePrefix { get; set; } = "sandbox";

    [Required]
    public string DefaultImage { get; set; } = "ghcr.io/andyjmorgan/codesandbox-executor:latest";

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

    public string AuthProxyImage { get; set; } = "ghcr.io/andyjmorgan/codesandbox-authproxy:latest";

    public ResourceConfig AuthProxySidecarResourceRequests { get; set; } = new() { MemoryMi = 64, CpuMillicores = 100 };
    public ResourceConfig AuthProxySidecarResourceLimits { get; set; } = new() { MemoryMi = 128, CpuMillicores = 250 };

    [Range(1, 65535, ErrorMessage = "Auth proxy port must be between 1 and 65535")]
    public int AuthProxyPort { get; set; } = 8080;

    [Range(1, 65535, ErrorMessage = "Auth proxy health port must be between 1 and 65535")]
    public int AuthProxyHealthPort { get; set; } = 8081;

    public List<string> AuthProxyBlockedDomains { get; set; } = new();

    public List<AuthProxyDomainCredentialConfig> AuthProxyDomainCredentials { get; set; } = new();

    public string AuthProxyCaSecretName { get; set; } = "sandbox-proxy-ca";

    // Persistent storage settings (single PVC with subPath mounts)
    public bool EnablePersistentStorage { get; set; } = true;

    public string StoragePvcName { get; set; } = "seaweedfs-buckets";

    public string UserFilesSubPathPrefix { get; set; } = "users";

    public string UserFilesMountPath { get; set; } = "/home/sandbox/files";

    public string SkillsSubPath { get; set; } = "skills";

    public string SkillsMountPath { get; set; } = "/home/sandbox/skills";

    // Optional: Direct k8s connection (alternative to kubeconfig)
    public KubernetesConnectionConfig? Connection { get; set; }
}

public class AuthProxyDomainCredentialConfig
{
    public string BaseDomain { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();
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
