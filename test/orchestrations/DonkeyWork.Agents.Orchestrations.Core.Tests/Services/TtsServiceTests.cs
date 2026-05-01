using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using DonkeyWork.Agents.Orchestrations.Core.Tests.Helpers;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using FileDownloadResult = DonkeyWork.Agents.Storage.Contracts.Models.FileDownloadResult;
using Moq;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Services;

public class TtsServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly TtsService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TtsServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        _storageServiceMock = new Mock<IStorageService>();
        _service = new TtsService(_dbContext, _storageServiceMock.Object, _identityContext);
    }

    public void Dispose() => _dbContext?.Dispose();

    #region ListRecordingsAsync Tests

    [Fact]
    public async Task ListRecordingsAsync_WithNoRecordings_ReturnsEmptyList()
    {
        var result = await _service.ListRecordingsAsync(0, 20);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ListRecordingsAsync_WithRecordings_ReturnsAll()
    {
        MockDbContext.SeedRecording(_dbContext, name: "Recording 1");
        MockDbContext.SeedRecording(_dbContext, name: "Recording 2");

        var result = await _service.ListRecordingsAsync(0, 20);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListRecordingsAsync_WithPagination_RespectsOffsetAndLimit()
    {
        for (var i = 0; i < 5; i++)
            MockDbContext.SeedRecording(_dbContext, name: $"Recording {i}");

        var result = await _service.ListRecordingsAsync(1, 2);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListRecordingsAsync_IncludesPlaybackState()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        MockDbContext.SeedPlayback(_dbContext, recording.Id, position: 45.0, completed: true);

        var result = await _service.ListRecordingsAsync(0, 20);

        Assert.Single(result.Items);
        Assert.NotNull(result.Items[0].Playback);
        Assert.Equal(45.0, result.Items[0].Playback!.PositionSeconds);
        Assert.True(result.Items[0].Playback.Completed);
    }

    [Fact]
    public async Task ListRecordingsAsync_OnlyReturnsCurrentUserRecordings()
    {
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        MockDbContext.SeedRecording(_dbContext, userId: _testUserId, name: "My Recording");
        MockDbContext.SeedRecording(_dbContext, userId: otherUserId, name: "Other Recording");

        var result = await _service.ListRecordingsAsync(0, 20);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("My Recording", result.Items[0].Name);
    }

    [Fact]
    public async Task ListRecordingsAsync_WithUnfiledOnly_ExcludesRecordingsInCollections()
    {
        var collection = MockDbContext.SeedAudioCollection(_dbContext);
        MockDbContext.SeedRecording(_dbContext, name: "Loose 1");
        MockDbContext.SeedRecording(_dbContext, name: "In Collection", collectionId: collection.Id);
        MockDbContext.SeedRecording(_dbContext, name: "Loose 2");

        var result = await _service.ListRecordingsAsync(0, 20, unfiledOnly: true);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, r => r.Name == "In Collection");
    }

    #endregion

    #region GetRecordingAsync Tests

    [Fact]
    public async Task GetRecordingAsync_WithValidId_ReturnsRecording()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);

        var result = await _service.GetRecordingAsync(recording.Id);

        Assert.NotNull(result);
        Assert.Equal(recording.Id, result.Id);
        Assert.Equal(recording.Name, result.Name);
        Assert.Equal(recording.Voice, result.Voice);
        Assert.Equal(recording.Model, result.Model);
    }

    [Fact]
    public async Task GetRecordingAsync_WithInvalidId_ReturnsNull()
    {
        var result = await _service.GetRecordingAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecordingAsync_OtherUsersRecording_ReturnsNull()
    {
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var recording = MockDbContext.SeedRecording(_dbContext, userId: otherUserId);

        var result = await _service.GetRecordingAsync(recording.Id);

        Assert.Null(result);
    }

    #endregion

    #region DownloadAudioAsync Tests

    [Fact]
    public async Task DownloadAudioAsync_WithValidRecording_ReturnsStream()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _storageServiceMock
            .Setup(s => s.DownloadAsync(recording.FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileDownloadResult { Content = audioStream, FileName = "test.mp3", ContentType = "audio/mpeg", SizeBytes = 3 });

        var result = await _service.DownloadAudioAsync(recording.Id);

        Assert.NotNull(result);
        Assert.Equal("audio/mpeg", result.Value.ContentType);
    }

    [Fact]
    public async Task DownloadAudioAsync_WithInvalidId_ReturnsNull()
    {
        var result = await _service.DownloadAudioAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAudioAsync_WhenStorageReturnsNull_ReturnsNull()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        _storageServiceMock
            .Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileDownloadResult?)null);

        var result = await _service.DownloadAudioAsync(recording.Id);

        Assert.Null(result);
    }

    #endregion

    #region GetPlaybackAsync Tests

    [Fact]
    public async Task GetPlaybackAsync_WithNoExistingPlayback_ReturnsDefaults()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);

        var result = await _service.GetPlaybackAsync(recording.Id);

        Assert.Equal(0, result.PositionSeconds);
        Assert.Equal(0, result.DurationSeconds);
        Assert.False(result.Completed);
        Assert.Equal(1.0, result.PlaybackSpeed);
    }

    [Fact]
    public async Task GetPlaybackAsync_WithExistingPlayback_ReturnsState()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        MockDbContext.SeedPlayback(_dbContext, recording.Id, position: 60.0, duration: 180.0);

        var result = await _service.GetPlaybackAsync(recording.Id);

        Assert.Equal(60.0, result.PositionSeconds);
        Assert.Equal(180.0, result.DurationSeconds);
    }

    #endregion

    #region UpdatePlaybackAsync Tests

    [Fact]
    public async Task UpdatePlaybackAsync_WithInvalidRecording_ReturnsNull()
    {
        var request = new UpdatePlaybackRequestV1
        {
            PositionSeconds = 10,
            DurationSeconds = 100
        };

        var result = await _service.UpdatePlaybackAsync(Guid.NewGuid(), request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePlaybackAsync_CreatesNewPlayback_WhenNoneExists()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        var request = new UpdatePlaybackRequestV1
        {
            PositionSeconds = 30,
            DurationSeconds = 120,
            Completed = false,
            PlaybackSpeed = 1.5
        };

        var result = await _service.UpdatePlaybackAsync(recording.Id, request);

        Assert.NotNull(result);
        Assert.Equal(30, result.PositionSeconds);
        Assert.Equal(120, result.DurationSeconds);
        Assert.Equal(1.5, result.PlaybackSpeed);
        Assert.False(result.Completed);
    }

    [Fact]
    public async Task UpdatePlaybackAsync_UpdatesExistingPlayback()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        MockDbContext.SeedPlayback(_dbContext, recording.Id, position: 10.0, duration: 100.0);

        var request = new UpdatePlaybackRequestV1
        {
            PositionSeconds = 90,
            DurationSeconds = 100,
            Completed = true,
            PlaybackSpeed = 2.0
        };

        var result = await _service.UpdatePlaybackAsync(recording.Id, request);

        Assert.NotNull(result);
        Assert.Equal(90, result.PositionSeconds);
        Assert.True(result.Completed);
        Assert.Equal(2.0, result.PlaybackSpeed);
    }

    #endregion

    #region DeleteRecordingAsync Tests

    [Fact]
    public async Task DeleteRecordingAsync_WithValidId_DeletesAndReturnsTrue()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        _storageServiceMock
            .Setup(s => s.DeleteAsync(recording.FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteRecordingAsync(recording.Id);

        Assert.True(result);
        var deleted = await _dbContext.TtsRecordings.FindAsync(recording.Id);
        Assert.Null(deleted);
        _storageServiceMock.Verify(
            s => s.DeleteAsync(recording.FilePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRecordingAsync_WithInvalidId_ReturnsFalse()
    {
        var result = await _service.DeleteRecordingAsync(Guid.NewGuid());

        Assert.False(result);
        _storageServiceMock.Verify(
            s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteRecordingAsync_CascadesPlaybackDeletion()
    {
        var recording = MockDbContext.SeedRecording(_dbContext);
        MockDbContext.SeedPlayback(_dbContext, recording.Id);
        _storageServiceMock
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteRecordingAsync(recording.Id);

        Assert.True(result);
        Assert.Empty(_dbContext.TtsPlayback.ToList());
    }

    #endregion
}
