using CodeSandbox.AuthProxy.Credentials;
using Xunit;

namespace CodeSandbox.AuthProxy.Tests.Credentials;

public class NullCredentialProviderTests
{
    [Fact]
    public async Task GetHeadersForDomainAsync_AlwaysReturnsNull()
    {
        var provider = new NullCredentialProvider();

        var result = await provider.GetHeadersForDomainAsync("example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHeadersForDomainAsync_ReturnsNullForAnyDomain()
    {
        var provider = new NullCredentialProvider();

        Assert.Null(await provider.GetHeadersForDomainAsync("api.github.com"));
        Assert.Null(await provider.GetHeadersForDomainAsync(""));
        Assert.Null(await provider.GetHeadersForDomainAsync("localhost"));
    }
}
