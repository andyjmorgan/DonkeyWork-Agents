using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Persistence.Entities.Tts;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public class TestDataSeeder
{
    private readonly AgentsDbContext _dbContext;

    public TestDataSeeder(AgentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region Agent Seeding

    public async Task<OrchestrationEntity> SeedAgentAsync(
        Guid userId,
        string name,
        string? description = null)
    {
        var agent = new OrchestrationEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test agent description"
        };

        _dbContext.Orchestrations.Add(agent);
        await _dbContext.SaveChangesAsync();
        return agent;
    }

    public async Task<OrchestrationVersionEntity> SeedAgentVersionAsync(
        Guid agentId,
        Guid userId,
        int versionNumber = 1,
        bool isDraft = true)
    {
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                new ReactFlowNode
                {
                    Id = startNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 100, Y = 100 },
                    Data = new ReactFlowNodeData { NodeType = NodeType.Start, Label = "start_1", DisplayName = "start_1" }
                },
                new ReactFlowNode
                {
                    Id = endNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 100, Y = 250 },
                    Data = new ReactFlowNodeData { NodeType = NodeType.End, Label = "end_1", DisplayName = "end_1" }
                }
            ],
            Edges =
            [
                new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = endNodeId }
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, NodeConfiguration>
        {
            [startNodeId] = new StartNodeConfiguration { Name = "start_1", InputSchema = JsonSerializer.SerializeToElement(new { type = "object" }) },
            [endNodeId] = new EndNodeConfiguration { Name = "end_1" }
        };

        var version = new OrchestrationVersionEntity
        {
            Id = Guid.NewGuid(),
            OrchestrationId = agentId,
            UserId = userId,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            InputSchema = JsonDocument.Parse("{}"),
            ReactFlowData = reactFlowData,
            NodeConfigurations = nodeConfigurations
        };

        _dbContext.OrchestrationVersions.Add(version);
        await _dbContext.SaveChangesAsync();
        return version;
    }

    #endregion

    #region Project Seeding

    public async Task<ProjectEntity> SeedProjectAsync(
        Guid userId,
        string name,
        string? content = null,
        ProjectStatus status = ProjectStatus.NotStarted)
    {
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Content = content ?? "Test project content",
            Status = status
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        return project;
    }

    public async Task<MilestoneEntity> SeedMilestoneAsync(
        Guid projectId,
        Guid userId,
        string name,
        string? content = null,
        MilestoneStatus status = MilestoneStatus.NotStarted)
    {
        var milestone = new MilestoneEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Name = name,
            Content = content ?? "Test milestone content",
            Status = status,
            SortOrder = 0
        };

        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync();
        return milestone;
    }

    #endregion

    #region API Key Seeding

    public async Task<UserApiKeyEntity> SeedUserApiKeyAsync(
        Guid userId,
        string name,
        string? description = null)
    {
        var apiKey = new UserApiKeyEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test API key",
            EncryptedKey = new byte[32]
        };

        _dbContext.UserApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync();
        return apiKey;
    }

    #endregion

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    #region TTS Seeding

    public async Task<TtsAudioCollectionEntity> SeedAudioCollectionAsync(
        Guid userId,
        string? name = null,
        Guid? id = null)
    {
        var now = DateTimeOffset.UtcNow;
        var collection = new TtsAudioCollectionEntity
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            Name = name ?? $"Seeded Collection {Guid.NewGuid().ToString("N")[..8]}",
            Description = "Seeded collection",
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.TtsAudioCollections.Add(collection);
        await _dbContext.SaveChangesAsync();
        return collection;
    }

    public async Task<TtsRecordingEntity> SeedTtsRecordingAsync(
        Guid userId,
        Guid? collectionId = null,
        int? sequenceNumber = null,
        string? chapterTitle = null,
        TtsRecordingStatus status = TtsRecordingStatus.Ready,
        string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        var recording = new TtsRecordingEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name ?? $"Seeded Recording {Guid.NewGuid().ToString("N")[..8]}",
            Description = "Seeded recording",
            FilePath = $"tts/{userId}/test/{Guid.NewGuid()}.mp3",
            Transcript = "Seeded transcript",
            ContentType = "audio/mpeg",
            SizeBytes = 1024,
            Voice = "alloy",
            Model = "tts-1",
            CollectionId = collectionId,
            SequenceNumber = sequenceNumber,
            ChapterTitle = chapterTitle,
            Status = status,
            Progress = status == TtsRecordingStatus.Ready ? 1.0 : 0.0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.TtsRecordings.Add(recording);
        await _dbContext.SaveChangesAsync();
        return recording;
    }

    #endregion
}
