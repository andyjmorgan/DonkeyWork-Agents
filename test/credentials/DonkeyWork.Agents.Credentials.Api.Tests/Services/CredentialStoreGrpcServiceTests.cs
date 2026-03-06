using CodeSandbox.Contracts.Grpc.Credentials;
using DonkeyWork.Agents.Credentials.Api.Services;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Api.Tests.Services;

public class CredentialStoreGrpcServiceTests
{
    private readonly Mock<ISandboxCredentialMappingService> _mappingServiceMock;
    private readonly Mock<ILogger<CredentialStoreGrpcService>> _loggerMock;
    private readonly CredentialStoreGrpcService _service;

    public CredentialStoreGrpcServiceTests()
    {
        _mappingServiceMock = new Mock<ISandboxCredentialMappingService>();
        _loggerMock = new Mock<ILogger<CredentialStoreGrpcService>>();
        _service = new CredentialStoreGrpcService(_mappingServiceMock.Object, _loggerMock.Object);
    }

    private static ServerCallContext CreateTestServerCallContext()
    {
        return new TestServerCallContext();
    }

    #region GetDomainCredentials Tests

    [Fact]
    public async Task GetDomainCredentials_DomainFound_ReturnsFoundWithHeaders()
    {
        // Arrange
        var request = new GetDomainCredentialsRequest { BaseDomain = "api.openai.com" };

        _mappingServiceMock
            .Setup(s => s.ResolveForDomainAsync("api.openai.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedDomainCredentialV1
            {
                BaseDomain = "api.openai.com",
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer sk-test-123" },
                    { "X-Custom", "custom-value" },
                },
            });

        // Act
        var result = await _service.GetDomainCredentials(request, CreateTestServerCallContext());

        // Assert
        Assert.True(result.Found);
        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("Bearer sk-test-123", result.Headers["Authorization"]);
        Assert.Equal("custom-value", result.Headers["X-Custom"]);
    }

    [Fact]
    public async Task GetDomainCredentials_DomainNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new GetDomainCredentialsRequest { BaseDomain = "unknown.com" };

        _mappingServiceMock
            .Setup(s => s.ResolveForDomainAsync("unknown.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedDomainCredentialV1?)null);

        // Act
        var result = await _service.GetDomainCredentials(request, CreateTestServerCallContext());

        // Assert
        Assert.False(result.Found);
        Assert.Empty(result.Headers);
    }

    #endregion

    /// <summary>
    /// Minimal ServerCallContext implementation for unit testing gRPC services.
    /// </summary>
    private class TestServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "test-peer";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }

        protected override AuthContext AuthContextCore =>
            new(null, new Dictionary<string, List<AuthProperty>>());

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotImplementedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;
    }
}
