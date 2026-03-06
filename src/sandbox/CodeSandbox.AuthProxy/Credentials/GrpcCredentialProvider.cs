using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using CodeSandbox.AuthProxy.Configuration;
using CodeSandbox.Contracts.Grpc.Credentials;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace CodeSandbox.AuthProxy.Credentials;

public class GrpcCredentialProvider : ICredentialProvider, IDisposable
{
    private readonly CredentialStoreService.CredentialStoreServiceClient _client;
    private readonly GrpcChannel _channel;
    private readonly string _userId;
    private readonly TimeSpan _cacheTtl;
    private readonly HashSet<string> _dynamicDomains;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<GrpcCredentialProvider> _logger;
    private readonly ICredentialProvider _staticFallback;

    public GrpcCredentialProvider(
        ProxyConfiguration config,
        ILogger<GrpcCredentialProvider> logger)
    {
        _logger = logger;
        _userId = config.CredentialStoreUserId ?? string.Empty;
        _cacheTtl = TimeSpan.FromSeconds(config.CredentialCacheTtlSeconds);
        _dynamicDomains = new HashSet<string>(config.DynamicCredentialDomains, StringComparer.OrdinalIgnoreCase);
        _staticFallback = new StaticCredentialProvider(config);

        var httpHandler = new HttpClientHandler();

        if (!string.IsNullOrEmpty(config.GrpcClientCertPath) &&
            !string.IsNullOrEmpty(config.GrpcClientKeyPath) &&
            File.Exists(config.GrpcClientCertPath) &&
            File.Exists(config.GrpcClientKeyPath))
        {
            var clientCert = X509Certificate2.CreateFromPemFile(config.GrpcClientCertPath, config.GrpcClientKeyPath);
            httpHandler.ClientCertificates.Add(clientCert);
            _logger.LogInformation("Loaded gRPC client certificate from {Path}", config.GrpcClientCertPath);
        }

        if (!string.IsNullOrEmpty(config.GrpcCaCertPath) && File.Exists(config.GrpcCaCertPath))
        {
            httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert is null) return false;
                var caCert = X509CertificateLoader.LoadCertificateFromFile(config.GrpcCaCertPath);
                using var customChain = new X509Chain();
                customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                customChain.ChainPolicy.CustomTrustStore.Add(caCert);
                customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return customChain.Build(new X509Certificate2(cert));
            };
            _logger.LogInformation("Loaded gRPC CA certificate from {Path}", config.GrpcCaCertPath);
        }

        _channel = GrpcChannel.ForAddress(config.CredentialStoreUrl!, new GrpcChannelOptions
        {
            HttpHandler = httpHandler,
        });
        _client = new CredentialStoreService.CredentialStoreServiceClient(_channel);

        _logger.LogInformation(
            "gRPC credential provider initialized: URL={Url}, UserId={UserId}, CacheTTL={TTL}s, DynamicDomains={Domains}",
            config.CredentialStoreUrl, _userId, config.CredentialCacheTtlSeconds,
            string.Join(", ", _dynamicDomains));
    }

    public async Task<Dictionary<string, string>?> GetHeadersForDomainAsync(string domain, CancellationToken ct = default)
    {
        // Check static credentials first (backward compatible)
        var staticHeaders = await _staticFallback.GetHeadersForDomainAsync(domain, ct);
        if (staticHeaders is not null)
            return staticHeaders;

        // Only attempt dynamic resolution for configured domains
        if (!_dynamicDomains.Contains(domain))
            return null;

        // Check cache
        if (_cache.TryGetValue(domain, out var cached) && !cached.IsExpired)
            return cached.Headers;

        // Fetch from gRPC
        try
        {
            var metadata = new Metadata { { "x-user-id", _userId } };
            var response = await _client.GetDomainCredentialsAsync(
                new GetDomainCredentialsRequest { BaseDomain = domain },
                headers: metadata,
                cancellationToken: ct);

            if (!response.Found)
            {
                _logger.LogDebug("No credentials found for domain {Domain}", domain);
                _cache[domain] = new CacheEntry(null, _cacheTtl);
                return null;
            }

            var headers = new Dictionary<string, string>(response.Headers);
            _cache[domain] = new CacheEntry(headers, _cacheTtl);

            _logger.LogDebug("Resolved {Count} header(s) for domain {Domain}", headers.Count, domain);
            return headers;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error resolving credentials for domain {Domain}: {Status}", domain, ex.StatusCode);
            return null;
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }

    private sealed class CacheEntry
    {
        public Dictionary<string, string>? Headers { get; }
        private readonly DateTimeOffset _expiresAt;

        public CacheEntry(Dictionary<string, string>? headers, TimeSpan ttl)
        {
            Headers = headers;
            _expiresAt = DateTimeOffset.UtcNow.Add(ttl);
        }

        public bool IsExpired => DateTimeOffset.UtcNow >= _expiresAt;
    }
}
