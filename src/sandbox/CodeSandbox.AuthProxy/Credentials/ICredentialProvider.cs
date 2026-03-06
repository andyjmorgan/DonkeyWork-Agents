namespace CodeSandbox.AuthProxy.Credentials;

public interface ICredentialProvider
{
    Task<Dictionary<string, string>?> GetHeadersForDomainAsync(string domain, CancellationToken ct = default);
}
