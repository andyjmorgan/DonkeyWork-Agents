using System.ComponentModel.DataAnnotations;

namespace CodeSandbox.AuthProxy.Configuration;

public class ProxyConfiguration
{
    [Range(1, 65535)]
    public int ProxyPort { get; set; } = 8080;

    [Range(1, 65535)]
    public int HealthPort { get; set; } = 8081;

    public List<string> BlockedDomains { get; set; } = new();

    public string CaCertificatePath { get; set; } = "/certs/ca.crt";

    public string CaPrivateKeyPath { get; set; } = "/certs/ca.key";

    public List<DomainCredentialConfig> DomainCredentials { get; set; } = new();

    public string? CredentialStoreUrl { get; set; }

    public string? CredentialStoreUserId { get; set; }

    public int CredentialCacheTtlSeconds { get; set; } = 300;

    public List<string> DynamicCredentialDomains { get; set; } = new();

    public string? GrpcClientCertPath { get; set; }

    public string? GrpcClientKeyPath { get; set; }

    public string? GrpcCaCertPath { get; set; }
}

public class DomainCredentialConfig
{
    public string BaseDomain { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();
}
