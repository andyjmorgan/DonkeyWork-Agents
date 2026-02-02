using DonkeyWork.Agents.Agents.Core.Execution.Providers;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using Microsoft.Extensions.Logging;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Execution.Providers;

/// <summary>
/// Unit tests for TimingNodeProvider.
/// Tests Sleep node execution.
/// </summary>
public class TimingNodeProviderTests
{
    private readonly Mock<ILogger<TimingNodeProvider>> _loggerMock;
    private readonly TimingNodeProvider _provider;

    public TimingNodeProviderTests()
    {
        _loggerMock = new Mock<ILogger<TimingNodeProvider>>();
        _provider = new TimingNodeProvider(_loggerMock.Object);
    }

    #region ExecuteSleepAsync Tests

    [Fact]
    public async Task ExecuteSleepAsync_WithValidConfig_ReturnsOutput()
    {
        // Arrange
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 0.01 };

        // Act
        var result = await _provider.ExecuteSleepAsync(config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0.01, result.DurationSeconds);
    }

    [Fact]
    public async Task ExecuteSleepAsync_WithZeroDuration_CompletesImmediately()
    {
        // Arrange
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 0 };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _provider.ExecuteSleepAsync(config, CancellationToken.None);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.DurationSeconds);
        Assert.True(sw.ElapsedMilliseconds < 100, "Zero duration sleep should complete quickly");
    }

    [Fact]
    public async Task ExecuteSleepAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 10 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await _provider.ExecuteSleepAsync(config, cts.Token)
        );
    }

    [Fact]
    public async Task ExecuteSleepAsync_OutputToMessageOutput_ReturnsFormattedString()
    {
        // Arrange
        var config = new SleepNodeConfiguration { Name = "sleep_1", DurationSeconds = 0.1 };

        // Act
        var result = await _provider.ExecuteSleepAsync(config, CancellationToken.None);
        var messageOutput = result.ToMessageOutput();

        // Assert
        Assert.NotNull(messageOutput);
        Assert.Contains("0.1", messageOutput);
    }

    #endregion
}
