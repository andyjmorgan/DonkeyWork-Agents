using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Tts;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DonkeyWork.Agents.Orchestrations.Core.Tests.Helpers;

public static class MockDbContext
{
    public static (AgentsDbContext DbContext, IIdentityContext IdentityContext) CreateWithIdentityContext(
        string? databaseName = null,
        Guid? userId = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        userId ??= Guid.Parse("11111111-1111-1111-1111-111111111111");

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var mockIdentityContext = new Mock<IIdentityContext>();
        mockIdentityContext.Setup(x => x.UserId).Returns(userId.Value);
        mockIdentityContext.Setup(x => x.Email).Returns("test@example.com");
        mockIdentityContext.Setup(x => x.Name).Returns("Test User");
        mockIdentityContext.Setup(x => x.Username).Returns("testuser");

        var context = new AgentsDbContext(options, mockIdentityContext.Object);

        return (context, mockIdentityContext.Object);
    }

    public static TtsRecordingEntity SeedRecording(
        AgentsDbContext context,
        Guid? userId = null,
        string name = "Test Recording",
        string filePath = "tts/test/audio.mp3",
        Guid? collectionId = null)
    {
        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = name,
            Description = "Test description",
            FilePath = filePath,
            Transcript = "Hello world",
            ContentType = "audio/mpeg",
            SizeBytes = 12345,
            Voice = "alloy",
            Model = "tts-1",
            CollectionId = collectionId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.TtsRecordings.Add(recording);
        context.SaveChanges();
        return recording;
    }

    public static TtsAudioCollectionEntity SeedAudioCollection(
        AgentsDbContext context,
        Guid? userId = null,
        string name = "Test Collection")
    {
        var collection = new TtsAudioCollectionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = name,
            Description = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        context.TtsAudioCollections.Add(collection);
        context.SaveChanges();
        return collection;
    }

    public static TtsPlaybackEntity SeedPlayback(
        AgentsDbContext context,
        Guid recordingId,
        Guid? userId = null,
        double position = 30.0,
        double duration = 120.0,
        bool completed = false)
    {
        var playback = new TtsPlaybackEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            RecordingId = recordingId,
            PositionSeconds = position,
            DurationSeconds = duration,
            Completed = completed,
            PlaybackSpeed = 1.0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.TtsPlayback.Add(playback);
        context.SaveChanges();
        return playback;
    }
}
