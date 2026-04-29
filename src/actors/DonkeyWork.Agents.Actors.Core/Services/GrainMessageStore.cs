using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Services;

/// <summary>
/// EF Core implementation of grain message persistence.
/// Uses IDbContextFactory to create short-lived DbContext instances,
/// avoiding long-lived connections in Orleans grains.
/// Handles JSON serialization of InternalMessage polymorphic hierarchy.
/// </summary>
public sealed class GrainMessageStore : IGrainMessageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    private readonly IDbContextFactory<AgentsDbContext> _dbContextFactory;
    private readonly ILogger<GrainMessageStore> _logger;

    public GrainMessageStore(
        IDbContextFactory<AgentsDbContext> dbContextFactory,
        ILogger<GrainMessageStore> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<(List<InternalMessage> Messages, int NextSequenceNumber)> LoadMessagesAsync(
        string grainKey, Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.GrainMessages
            .IgnoreQueryFilters()
            .Where(e => e.GrainKey == grainKey && e.UserId == userId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync(ct);

        var messages = new List<InternalMessage>(entities.Count);
        foreach (var entity in entities)
        {
            var msg = JsonSerializer.Deserialize<InternalMessage>(entity.MessageJson, JsonOptions);
            if (msg is not null)
                messages.Add(msg);
        }

        var nextSeq = entities.Count > 0 ? entities[^1].SequenceNumber + 1 : 0;

        _logger.LogDebug(
            "Loaded {Count} messages for grain {GrainKey}, nextSeq={NextSeq}",
            messages.Count, grainKey, nextSeq);

        return (messages, nextSeq);
    }

    public async Task<int> AppendMessageAsync(
        string grainKey, Guid userId, InternalMessage message, int sequenceNumber, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            dbContext.GrainMessages.Add(new GrainMessageEntity
            {
                GrainKey = grainKey,
                UserId = userId,
                SequenceNumber = sequenceNumber,
                MessageJson = json,
                TurnId = message.TurnId,
            });

            await dbContext.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Appended message seq={Seq} for grain {GrainKey}",
                sequenceNumber, grainKey);

            return sequenceNumber + 1;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            _logger.LogWarning(
                "Duplicate sequence number {Seq} for grain {GrainKey}, resolving from database",
                sequenceNumber, grainKey);

            // Determine the correct next sequence number from the database
            await using var freshContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var maxSeq = await freshContext.GrainMessages
                .IgnoreQueryFilters()
                .Where(e => e.GrainKey == grainKey && e.UserId == userId)
                .MaxAsync(e => (int?)e.SequenceNumber, ct) ?? -1;

            var correctedSeq = maxSeq + 1;

            freshContext.GrainMessages.Add(new GrainMessageEntity
            {
                GrainKey = grainKey,
                UserId = userId,
                SequenceNumber = correctedSeq,
                MessageJson = json,
                TurnId = message.TurnId,
            });

            await freshContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Recovered: appended message at corrected seq={Seq} for grain {GrainKey}",
                correctedSeq, grainKey);

            return correctedSeq + 1;
        }
    }

    public async Task RollbackFromAsync(
        string grainKey, Guid userId, int fromSequenceNumber, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var deleted = await dbContext.GrainMessages
            .IgnoreQueryFilters()
            .Where(e => e.GrainKey == grainKey && e.UserId == userId && e.SequenceNumber >= fromSequenceNumber)
            .ExecuteDeleteAsync(ct);

        _logger.LogDebug(
            "Rolled back {Count} messages from seq={FromSeq} for grain {GrainKey}",
            deleted, fromSequenceNumber, grainKey);
    }
}
