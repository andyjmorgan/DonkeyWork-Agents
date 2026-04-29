using System.Net;
using System.Net.Http.Json;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class AudioCollectionsControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/audio-collections";
    private const string TtsBaseUrl = "/api/v1/tts";

    public AudioCollectionsControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedCollection()
    {
        var request = TestDataBuilder.CreateAudioCollectionRequest(
            name: "Daily AI News",
            description: "All daily AI news recordings",
            defaultVoice: "alloy",
            defaultModel: "tts-1");

        var response = await PostResponseAsync(BaseUrl, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var collection = await response.Content.ReadFromJsonAsync<AudioCollectionV1>(JsonOptions);
        Assert.NotNull(collection);
        Assert.NotEqual(Guid.Empty, collection.Id);
        Assert.Equal("Daily AI News", collection.Name);
        Assert.Equal("alloy", collection.DefaultVoice);
        Assert.Equal("tts-1", collection.DefaultModel);
        Assert.Equal(0, collection.RecordingCount);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_WithNoCollections_ReturnsEmptyList()
    {
        var response = await GetAsync<ListAudioCollectionsResponseV1>(BaseUrl);

        Assert.NotNull(response);
        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task List_WithMultipleCollections_ReturnsAll()
    {
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "A"));
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "B"));
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "C"));

        var response = await GetAsync<ListAudioCollectionsResponseV1>(BaseUrl);

        Assert.NotNull(response);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.Items.Count);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingCollection_ReturnsCollection()
    {
        var created = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "My Folder"));

        var fetched = await GetAsync<AudioCollectionV1>($"{BaseUrl}/{created!.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("My Folder", fetched.Name);
    }

    [Fact]
    public async Task Get_NonexistentCollection_ReturnsNotFound()
    {
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingCollection_AppliesOnlyProvidedFields()
    {
        var created = await PostAsync<AudioCollectionV1>(BaseUrl,
            TestDataBuilder.CreateAudioCollectionRequest(name: "Original", description: "Original desc"));

        var updateRequest = TestDataBuilder.UpdateAudioCollectionRequest(name: "Renamed");

        var updated = await PutAsync<AudioCollectionV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal("Original desc", updated.Description);
    }

    [Fact]
    public async Task Update_NonexistentCollection_ReturnsNotFound()
    {
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}",
            TestDataBuilder.UpdateAudioCollectionRequest(name: "X"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingCollection_ReturnsNoContent()
    {
        var created = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        var response = await Client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CollectionWithRecordings_RecordingsBecomeUnfiled()
    {
        var user = TestUser.CreateRandom();
        SetTestUser(user);

        var created = await PostAsync<AudioCollectionV1>(BaseUrl,
            TestDataBuilder.CreateAudioCollectionRequest(name: "ToDelete"));

        TtsRecordingV1 recordingBefore;
        await using (var seederCtx = CreateDbContext())
        {
            var seeder = new TestDataSeeder(seederCtx);
            var entity = await seeder.SeedTtsRecordingAsync(user.UserId, collectionId: created!.Id, sequenceNumber: 1);
            recordingBefore = (await GetAsync<TtsRecordingV1>($"{TtsBaseUrl}/recordings/{entity.Id}"))!;
        }
        Assert.Equal(created!.Id, recordingBefore.CollectionId);

        var deleteResponse = await Client.DeleteAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var recordingAfter = await GetAsync<TtsRecordingV1>($"{TtsBaseUrl}/recordings/{recordingBefore.Id}");
        Assert.NotNull(recordingAfter);
        Assert.Null(recordingAfter.CollectionId);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_CollectionOwnedByAnotherUser_ReturnsNotFound()
    {
        var userA = TestUser.CreateRandom();
        var userB = TestUser.CreateRandom();

        SetTestUser(userA);
        var created = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        SetTestUser(userB);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_CollectionOwnedByAnotherUser_ReturnsNotFound()
    {
        var userA = TestUser.CreateRandom();
        var userB = TestUser.CreateRandom();

        SetTestUser(userA);
        var created = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        SetTestUser(userB);
        var response = await PutResponseAsync($"{BaseUrl}/{created!.Id}",
            TestDataBuilder.UpdateAudioCollectionRequest(name: "hijack"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOnlyCollectionsOwnedByCurrentUser()
    {
        var userA = TestUser.CreateRandom();
        var userB = TestUser.CreateRandom();

        SetTestUser(userA);
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "A-owned"));
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "A-owned-2"));

        SetTestUser(userB);
        await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest(name: "B-owned"));

        var aList = await GetAsync<ListAudioCollectionsResponseV1>(BaseUrl + "?offset=0&limit=50");
        SetTestUser(userA);
        aList = await GetAsync<ListAudioCollectionsResponseV1>(BaseUrl + "?offset=0&limit=50");

        Assert.NotNull(aList);
        Assert.Equal(2, aList.TotalCount);
        Assert.All(aList.Items, c => Assert.StartsWith("A-owned", c.Name));
    }

    #endregion

    #region Recordings In Collection

    [Fact]
    public async Task ListRecordings_EmptyCollection_ReturnsEmpty()
    {
        var created = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        var result = await GetAsync<ListRecordingsResponseV1>($"{BaseUrl}/{created!.Id}/recordings");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ListRecordings_NonexistentCollection_ReturnsNotFound()
    {
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}/recordings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListRecordings_OrderedBySequenceNumber()
    {
        var user = TestUser.CreateRandom();
        SetTestUser(user);

        var collection = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        await using (var ctx = CreateDbContext())
        {
            var seeder = new TestDataSeeder(ctx);
            await seeder.SeedTtsRecordingAsync(user.UserId, collection!.Id, sequenceNumber: 2, name: "second");
            await seeder.SeedTtsRecordingAsync(user.UserId, collection.Id, sequenceNumber: 1, name: "first");
            await seeder.SeedTtsRecordingAsync(user.UserId, collection.Id, sequenceNumber: 3, name: "third");
        }

        var result = await GetAsync<ListRecordingsResponseV1>($"{BaseUrl}/{collection!.Id}/recordings");

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Collection(
            result.Items,
            r => Assert.Equal("first", r.Name),
            r => Assert.Equal("second", r.Name),
            r => Assert.Equal("third", r.Name));
    }

    #endregion

    #region Move Recording

    [Fact]
    public async Task MoveRecording_ToCollection_AppendsAtEndWhenNoSequenceGiven()
    {
        var user = TestUser.CreateRandom();
        SetTestUser(user);

        var collection = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        Guid recordingId;
        await using (var ctx = CreateDbContext())
        {
            var seeder = new TestDataSeeder(ctx);
            await seeder.SeedTtsRecordingAsync(user.UserId, collection!.Id, sequenceNumber: 1, name: "existing");
            var newRecording = await seeder.SeedTtsRecordingAsync(user.UserId, collectionId: null, name: "new");
            recordingId = newRecording.Id;
        }

        var moved = await PutAsync<TtsRecordingV1>(
            $"{TtsBaseUrl}/recordings/{recordingId}/collection",
            TestDataBuilder.MoveRecordingRequest(collection!.Id));

        Assert.NotNull(moved);
        Assert.Equal(collection.Id, moved.CollectionId);
        Assert.Equal(2, moved.SequenceNumber);
    }

    [Fact]
    public async Task MoveRecording_ToNullCollection_UnfilesTheRecording()
    {
        var user = TestUser.CreateRandom();
        SetTestUser(user);

        var collection = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        Guid recordingId;
        await using (var ctx = CreateDbContext())
        {
            var seeder = new TestDataSeeder(ctx);
            var r = await seeder.SeedTtsRecordingAsync(user.UserId, collection!.Id, sequenceNumber: 1);
            recordingId = r.Id;
        }

        var moved = await PutAsync<TtsRecordingV1>(
            $"{TtsBaseUrl}/recordings/{recordingId}/collection",
            TestDataBuilder.MoveRecordingRequest(collectionId: null));

        Assert.NotNull(moved);
        Assert.Null(moved.CollectionId);
        Assert.Null(moved.SequenceNumber);
    }

    [Fact]
    public async Task MoveRecording_NonexistentRecording_ReturnsNotFound()
    {
        var collection = await PostAsync<AudioCollectionV1>(BaseUrl, TestDataBuilder.CreateAudioCollectionRequest());

        var response = await PutResponseAsync(
            $"{TtsBaseUrl}/recordings/{Guid.NewGuid()}/collection",
            TestDataBuilder.MoveRecordingRequest(collection!.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
