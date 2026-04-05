using CodeSandbox.AuthProxy.Configuration;
using CodeSandbox.AuthProxy.Credentials;
using CodeSandbox.AuthProxy.Proxy;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSandbox.AuthProxy.Tests.Proxy;

public class ProxyServerTests
{
    #region ParseConnectRequest Tests

    [Fact]
    public void ParseConnectRequest_ValidHostAndPort_ParsesCorrectly()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("CONNECT example.com:443 HTTP/1.1");

        Assert.Equal("CONNECT", method);
        Assert.Equal("example.com", host);
        Assert.Equal(443, port);
    }

    [Fact]
    public void ParseConnectRequest_CustomPort_ParsesPort()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("CONNECT api.example.com:8443 HTTP/1.1");

        Assert.Equal("CONNECT", method);
        Assert.Equal("api.example.com", host);
        Assert.Equal(8443, port);
    }

    [Fact]
    public void ParseConnectRequest_NoPort_DefaultsTo443()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("CONNECT example.com HTTP/1.1");

        Assert.Equal("CONNECT", method);
        Assert.Equal("example.com", host);
        Assert.Equal(443, port);
    }

    [Fact]
    public void ParseConnectRequest_EmptyString_ReturnsNulls()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("");

        Assert.Null(method);
        Assert.Null(host);
        Assert.Equal(0, port);
    }

    [Fact]
    public void ParseConnectRequest_MethodOnly_ReturnsMethodAndTarget()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("GET");

        Assert.Null(method);
        Assert.Null(host);
        Assert.Equal(0, port);
    }

    [Fact]
    public void ParseConnectRequest_NonConnectMethod_StillParses()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("GET example.com:80 HTTP/1.1");

        Assert.Equal("GET", method);
        Assert.Equal("example.com", host);
        Assert.Equal(80, port);
    }

    [Fact]
    public void ParseConnectRequest_IPv6Address_ParsesCorrectly()
    {
        var (method, host, port) = ProxyServer.ParseConnectRequest("CONNECT [::1]:443 HTTP/1.1");

        Assert.Equal("CONNECT", method);
        Assert.Equal("[::1]", host);
        Assert.Equal(443, port);
    }

    #endregion

    #region IsDomainBlocked Tests

    [Fact]
    public void IsDomainBlocked_BlockedDomain_ReturnsTrue()
    {
        var server = CreateProxyServer(blockedDomains: ["evil.com", "malware.net"]);

        Assert.True(server.IsDomainBlocked("evil.com"));
    }

    [Fact]
    public void IsDomainBlocked_UnblockedDomain_ReturnsFalse()
    {
        var server = CreateProxyServer(blockedDomains: ["evil.com"]);

        Assert.False(server.IsDomainBlocked("example.com"));
    }

    [Fact]
    public void IsDomainBlocked_CaseInsensitive()
    {
        var server = CreateProxyServer(blockedDomains: ["Evil.Com"]);

        Assert.True(server.IsDomainBlocked("evil.com"));
        Assert.True(server.IsDomainBlocked("EVIL.COM"));
    }

    [Fact]
    public void IsDomainBlocked_SubdomainNotBlocked()
    {
        var server = CreateProxyServer(blockedDomains: ["evil.com"]);

        Assert.False(server.IsDomainBlocked("sub.evil.com"));
    }

    [Fact]
    public void IsDomainBlocked_EmptyBlocklist_NothingBlocked()
    {
        var server = CreateProxyServer(blockedDomains: []);

        Assert.False(server.IsDomainBlocked("anything.com"));
    }

    #endregion

    private static ProxyServer CreateProxyServer(List<string> blockedDomains)
    {
        var config = new ProxyConfiguration { BlockedDomains = blockedDomains };
        var caCert = CertificateGenerator.GenerateEphemeralCa();
        var certGen = new CertificateGenerator(caCert, Mock.Of<ILogger<CertificateGenerator>>());
        var mitmHandler = new TlsMitmHandler(certGen, Mock.Of<ILogger<TlsMitmHandler>>());

        return new ProxyServer(
            config,
            mitmHandler,
            new NullCredentialProvider(),
            Mock.Of<ILogger<ProxyServer>>());
    }
}
