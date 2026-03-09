namespace CodeSandbox.AuthProxy.Credentials;

public class NullCredentialProvider : ICredentialProvider
{
    public Task<Dictionary<string, string>?> GetHeadersForDomainAsync(string domain, CancellationToken ct = default)
        => Task.FromResult<Dictionary<string, string>?>(null);
}
