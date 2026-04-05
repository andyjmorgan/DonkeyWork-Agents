using DonkeyWork.Agents.Orchestrations.Contracts;
using DonkeyWork.Agents.Orchestrations.Core.Options;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class UserStreamManagerTests
{
    private readonly Mock<INatsJSContext> _jsContextMock;
    private readonly Mock<ILogger<UserStreamManager>> _loggerMock;
    private readonly UserStreamManager _manager;

    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UserStreamManagerTests()
    {
        _jsContextMock = new Mock<INatsJSContext>();
        _loggerMock = new Mock<ILogger<UserStreamManager>>();

        var options = Microsoft.Extensions.Options.Options.Create(new OrchestrationsOptions
        {
            StreamRetention = TimeSpan.FromHours(24),
            StreamMaxBytes = 1_073_741_824
        });

        _manager = new UserStreamManager(_jsContextMock.Object, options, _loggerMock.Object);
    }

    #region EnsureStreamAsync Tests

    [Fact]
    public async Task EnsureStreamAsync_FirstCall_CreatesStream()
    {
        _jsContextMock
            .Setup(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((INatsJSStream)null!);

        await _manager.EnsureStreamAsync(_testUserId);

        _jsContextMock.Verify(
            x => x.CreateOrUpdateStreamAsync(
                It.Is<StreamConfig>(c =>
                    c.Name == NatsSubjects.UserStream(_testUserId) &&
                    c.MaxAge == TimeSpan.FromHours(24) &&
                    c.MaxBytes == 1_073_741_824 &&
                    c.Retention == StreamConfigRetention.Limits &&
                    c.Discard == StreamConfigDiscard.Old),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStreamAsync_SubjectFilterIncludesUserId()
    {
        _jsContextMock
            .Setup(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((INatsJSStream)null!);

        await _manager.EnsureStreamAsync(_testUserId);

        _jsContextMock.Verify(
            x => x.CreateOrUpdateStreamAsync(
                It.Is<StreamConfig>(c =>
                    c.Subjects != null &&
                    c.Subjects.Contains(NatsSubjects.UserSubjectFilter(_testUserId))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStreamAsync_SecondCallForSameUser_SkipsCreation()
    {
        _jsContextMock
            .Setup(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((INatsJSStream)null!);

        await _manager.EnsureStreamAsync(_testUserId);
        await _manager.EnsureStreamAsync(_testUserId);

        _jsContextMock.Verify(
            x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureStreamAsync_DifferentUsers_CreatesStreamForEach()
    {
        var userId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        _jsContextMock
            .Setup(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((INatsJSStream)null!);

        await _manager.EnsureStreamAsync(_testUserId);
        await _manager.EnsureStreamAsync(userId2);

        _jsContextMock.Verify(
            x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EnsureStreamAsync_NatsThrows_PropagatesException()
    {
        _jsContextMock
            .Setup(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("NATS unavailable"));

        await Assert.ThrowsAsync<Exception>(
            () => _manager.EnsureStreamAsync(_testUserId));
    }

    [Fact]
    public async Task EnsureStreamAsync_NatsThrows_DoesNotCacheFailedAttempt()
    {
        _jsContextMock
            .SetupSequence(x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("NATS unavailable"))
            .ReturnsAsync((INatsJSStream)null!);

        await Assert.ThrowsAsync<Exception>(
            () => _manager.EnsureStreamAsync(_testUserId));

        await _manager.EnsureStreamAsync(_testUserId);

        _jsContextMock.Verify(
            x => x.CreateOrUpdateStreamAsync(
                It.IsAny<StreamConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion
}
