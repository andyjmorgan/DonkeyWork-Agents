using CodeSandbox.AuthProxy.Proxy;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSandbox.AuthProxy.Tests.Proxy;

public class CertificateGeneratorTests : IDisposable
{
    private readonly CertificateGenerator _generator;

    public CertificateGeneratorTests()
    {
        var caCert = CertificateGenerator.GenerateEphemeralCa();
        _generator = new CertificateGenerator(caCert, Mock.Of<ILogger<CertificateGenerator>>());
    }

    public void Dispose() => _generator.Dispose();

    #region GenerateEphemeralCa Tests

    [Fact]
    public void GenerateEphemeralCa_ReturnsCertificateWithPrivateKey()
    {
        using var cert = CertificateGenerator.GenerateEphemeralCa();

        Assert.True(cert.HasPrivateKey);
    }

    [Fact]
    public void GenerateEphemeralCa_IsCertificateAuthority()
    {
        using var cert = CertificateGenerator.GenerateEphemeralCa();

        var basicConstraints = cert.Extensions
            .OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>()
            .FirstOrDefault();

        Assert.NotNull(basicConstraints);
        Assert.True(basicConstraints.CertificateAuthority);
    }

    [Fact]
    public void GenerateEphemeralCa_HasExpectedSubject()
    {
        using var cert = CertificateGenerator.GenerateEphemeralCa();

        Assert.Contains("CodeSandbox Internal CA", cert.Subject);
    }

    #endregion

    #region GetOrCreateCertificate Tests

    [Fact]
    public void GetOrCreateCertificate_ReturnsCertForHostname()
    {
        var cert = _generator.GetOrCreateCertificate("example.com");

        Assert.NotNull(cert);
        Assert.True(cert.HasPrivateKey);
        Assert.Contains("example.com", cert.Subject);
    }

    [Fact]
    public void GetOrCreateCertificate_CachesResult()
    {
        var cert1 = _generator.GetOrCreateCertificate("cached.example.com");
        var cert2 = _generator.GetOrCreateCertificate("cached.example.com");

        Assert.Same(cert1, cert2);
    }

    [Fact]
    public void GetOrCreateCertificate_DifferentHostsReturnDifferentCerts()
    {
        var cert1 = _generator.GetOrCreateCertificate("host-a.example.com");
        var cert2 = _generator.GetOrCreateCertificate("host-b.example.com");

        Assert.NotSame(cert1, cert2);
        Assert.Contains("host-a.example.com", cert1.Subject);
        Assert.Contains("host-b.example.com", cert2.Subject);
    }

    [Fact]
    public void GetOrCreateCertificate_IsNotCertificateAuthority()
    {
        var cert = _generator.GetOrCreateCertificate("leaf.example.com");

        var basicConstraints = cert.Extensions
            .OfType<System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension>()
            .FirstOrDefault();

        Assert.NotNull(basicConstraints);
        Assert.False(basicConstraints.CertificateAuthority);
    }

    #endregion
}
