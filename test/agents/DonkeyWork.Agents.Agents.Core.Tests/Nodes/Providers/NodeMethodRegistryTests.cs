using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using Xunit;

namespace DonkeyWork.Agents.Agents.Core.Tests.Nodes.Providers;

/// <summary>
/// Unit tests for NodeMethodRegistry.
/// Tests provider discovery, method registration, and error handling.
/// </summary>
public class NodeMethodRegistryTests
{
    #region DiscoverMethods Tests

    [Fact]
    public void DiscoverMethods_WithValidProvider_RegistersMethod()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act
        registry.DiscoverMethods(typeof(TestProvider));

        // Assert
        Assert.True(registry.HasMethod(NodeType.Sleep));
        var methodInfo = registry.GetMethod(NodeType.Sleep);
        Assert.Equal(typeof(TestProvider), methodInfo.ProviderType);
        Assert.Equal(typeof(SleepNodeConfiguration), methodInfo.ConfigType);
        Assert.Equal(typeof(TestOutput), methodInfo.OutputType);
        Assert.True(methodInfo.HasCancellationToken);
    }

    [Fact]
    public void DiscoverMethods_WithMethodWithoutCancellationToken_SetsHasCancellationTokenFalse()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act
        registry.DiscoverMethods(typeof(TestProviderNoCancellation));

        // Assert
        var methodInfo = registry.GetMethod(NodeType.Sleep);
        Assert.False(methodInfo.HasCancellationToken);
    }

    [Fact]
    public void DiscoverMethods_WithMultipleMethods_RegistersAll()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act
        registry.DiscoverMethods(typeof(MultiMethodProvider));

        // Assert
        Assert.True(registry.HasMethod(NodeType.Sleep));
        Assert.True(registry.HasMethod(NodeType.MessageFormatter));
        Assert.Equal(2, registry.Methods.Count);
    }

    [Fact]
    public void DiscoverMethods_WithNoParameters_ThrowsException()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverMethods(typeof(InvalidProviderNoParams)));
        Assert.Contains("must have at least one parameter", exception.Message);
    }

    [Fact]
    public void DiscoverMethods_WithNonTaskReturnType_ThrowsException()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.DiscoverMethods(typeof(InvalidProviderWrongReturn)));
        Assert.Contains("must return Task<T>", exception.Message);
    }

    #endregion

    #region GetMethod Tests

    [Fact]
    public void GetMethod_WithRegisteredNodeType_ReturnsMethodInfo()
    {
        // Arrange
        var registry = new NodeMethodRegistry();
        registry.DiscoverMethods(typeof(TestProvider));

        // Act
        var methodInfo = registry.GetMethod(NodeType.Sleep);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(NodeType.Sleep, methodInfo.NodeType);
    }

    [Fact]
    public void GetMethod_WithUnregisteredNodeType_ThrowsArgumentException()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => registry.GetMethod(NodeType.Sleep));
        Assert.Contains("No method registered for node type", exception.Message);
    }

    #endregion

    #region HasMethod Tests

    [Fact]
    public void HasMethod_WithRegisteredNodeType_ReturnsTrue()
    {
        // Arrange
        var registry = new NodeMethodRegistry();
        registry.DiscoverMethods(typeof(TestProvider));

        // Act
        var result = registry.HasMethod(NodeType.Sleep);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasMethod_WithUnregisteredNodeType_ReturnsFalse()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act
        var result = registry.HasMethod(NodeType.Sleep);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DiscoverProviders Tests

    [Fact]
    public void DiscoverProviders_FromAssembly_FindsProviders()
    {
        // Arrange
        var registry = new NodeMethodRegistry();

        // Act - discover from this test assembly
        registry.DiscoverProviders(typeof(NodeMethodRegistryTests).Assembly);

        // Assert - should find TestProvider
        Assert.True(registry.HasMethod(NodeType.Sleep));
    }

    #endregion

    #region Test Providers

    [NodeProvider]
    public class TestProvider
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<TestOutput> ExecuteSleepAsync(SleepNodeConfiguration config, CancellationToken ct)
        {
            return Task.FromResult(new TestOutput());
        }
    }

    [NodeProvider]
    public class TestProviderNoCancellation
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<TestOutput> ExecuteSleepAsync(SleepNodeConfiguration config)
        {
            return Task.FromResult(new TestOutput());
        }
    }

    [NodeProvider]
    public class MultiMethodProvider
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<TestOutput> ExecuteSleepAsync(SleepNodeConfiguration config, CancellationToken ct)
        {
            return Task.FromResult(new TestOutput());
        }

        [NodeMethod(NodeType.MessageFormatter)]
        public Task<TestOutput> ExecuteFormatterAsync(MessageFormatterNodeConfiguration config, CancellationToken ct)
        {
            return Task.FromResult(new TestOutput());
        }
    }

    public class InvalidProviderNoParams
    {
        [NodeMethod(NodeType.Sleep)]
        public Task<TestOutput> ExecuteSleepAsync()
        {
            return Task.FromResult(new TestOutput());
        }
    }

    public class InvalidProviderWrongReturn
    {
        [NodeMethod(NodeType.Sleep)]
        public TestOutput ExecuteSync(SleepNodeConfiguration config)
        {
            return new TestOutput();
        }
    }

    public class TestOutput : NodeOutput
    {
        public override string ToMessageOutput() => "test";
    }

    #endregion
}
