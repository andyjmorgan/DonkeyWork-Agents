using CodeSandbox.AuthProxy.Configuration;

namespace CodeSandbox.AuthProxy.Credentials;

public class StaticCredentialProvider : ICredentialProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _domainHeaders;

    public StaticCredentialProvider(ProxyConfiguration config)
    {
        _domainHeaders = config.DomainCredentials
            .Where(dc => !string.IsNullOrEmpty(dc.BaseDomain) && dc.Headers.Count > 0)
            .ToDictionary(dc => dc.BaseDomain, dc => dc.Headers, StringComparer.OrdinalIgnoreCase);
    }

    public Task<Dictionary<string, string>?> GetHeadersForDomainAsync(string domain, CancellationToken ct = default)
    {
        _domainHeaders.TryGetValue(domain, out var headers);
        return Task.FromResult(headers);
    }
}
