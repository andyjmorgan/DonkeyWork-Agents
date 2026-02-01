using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution;

/// <summary>
/// Unit tests for GenericNodeExecutor.
/// Tests routing to provider methods and error handling.
/// </summary>
public class GenericNodeExecutorTests
{
    private readonly Mock<IExecutionStreamWriter> _streamWriterMock;
    private readonly NodeMethodRegistry _methodRegistry;
    private readonly IServiceProvider _serviceProvider;

    public GenericNodeExecutorTests()
    {
        _streamWriterMock = new Mock<IExecutionStreamWriter>();
        _streamWriterMock.Setup(s => s.WriteEventAsync(It.IsAny<ExecutionEvent>())).Returns(Task.CompletedTask);

        _methodRegistry = new NodeMethodRegistry();
        _methodRegistry.DiscoverMethods(typeof(TestSleepProvider));

        var services = new ServiceCollection();
        services.AddSingleton<TestSleepProvider>();
        _serviceProvider = services.BuildServiceProvider();
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithValidConfig_InvokesProviderMethod()
    {
        // Arrange
        var executor = new GenericNodeExecutor(_methodRegistry, _serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act
        var result = await executor.ExecuteAsync("node-1", config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SleepNodeOutput>(result);
        var sleepOutput = (SleepNodeOutput)result;
        Assert.Equal(10, sleepOutput.DurationMs);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonNodeConfiguration_ThrowsException()
    {
        // Arrange
        var executor = new GenericNodeExecutor(_methodRegistry, _serviceProvider, _streamWriterMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync("node-1", new { invalid = "config" }, CancellationToken.None)
        );

        Assert.Contains("must be a NodeConfiguration", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnregisteredNodeType_ThrowsException()
    {
        // Arrange
        var emptyRegistry = new NodeMethodRegistry();
        var executor = new GenericNodeExecutor(emptyRegistry, _serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await executor.ExecuteAsync("node-1", config, CancellationToken.None)
        );

        Assert.Contains("No method registered", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnregisteredProvider_ThrowsException()
    {
        // Arrange
        var emptyServices = new ServiceCollection().BuildServiceProvider();
        var executor = new GenericNodeExecutor(_methodRegistry, emptyServices, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync("node-1", config, CancellationToken.None)
        );

        Assert.Contains("not registered in DI", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsNodeStartedEvent()
    {
        // Arrange
        var executor = new GenericNodeExecutor(_methodRegistry, _serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act
        await executor.ExecuteAsync("test-node-id", config, CancellationToken.None);

        // Assert
        _streamWriterMock.Verify(
            s => s.WriteEventAsync(It.IsAny<NodeStartedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmitsNodeCompletedEvent()
    {
        // Arrange
        var executor = new GenericNodeExecutor(_methodRegistry, _serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act
        await executor.ExecuteAsync("test-node-id", config, CancellationToken.None);

        // Assert
        _streamWriterMock.Verify(
            s => s.WriteEventAsync(It.IsAny<NodeCompletedEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithProviderException_WrapsException()
    {
        // Arrange
        var failingRegistry = new NodeMethodRegistry();
        failingRegistry.DiscoverMethods(typeof(FailingProvider));

        var services = new ServiceCollection();
        services.AddSingleton<FailingProvider>();
        var serviceProvider = services.BuildServiceProvider();

        var executor = new GenericNodeExecutor(failingRegistry, serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync("node-1", config, CancellationToken.None)
        );

        Assert.Contains("Node execution failed", exception.Message);
        Assert.Contains("Test error", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var cancellationRegistry = new NodeMethodRegistry();
        cancellationRegistry.DiscoverMethods(typeof(CancellationAwareProvider));

        var services = new ServiceCollection();
        services.AddSingleton<CancellationAwareProvider>();
        var serviceProvider = services.BuildServiceProvider();

        var executor = new GenericNodeExecutor(cancellationRegistry, serviceProvider, _streamWriterMock.Object);
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationMs = 10000 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync("node-1", config, cts.Token)
        );
    }

    #endregion

    #region Test Providers

    [NodeProvider]
    public class TestSleepProvider
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<SleepNodeOutput> ExecuteSleepAsync(SleepNodeConfiguration config, CancellationToken ct)
        {
            return Task.FromResult(new SleepNodeOutput { DurationMs = config.DurationMs });
        }
    }

    [NodeProvider]
    public class FailingProvider
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<SleepNodeOutput> ExecuteSleepAsync(SleepNodeConfiguration config, CancellationToken ct)
        {
            throw new InvalidOperationException("Test error");
        }
    }

    [NodeProvider]
    public class CancellationAwareProvider
    {
        [NodeMethod(NodeType.Sleep)]
        public async Task<SleepNodeOutput> ExecuteSleepAsync(SleepNodeConfiguration config, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct);
            return new SleepNodeOutput { DurationMs = config.DurationMs };
        }
    }

    #endregion
}
