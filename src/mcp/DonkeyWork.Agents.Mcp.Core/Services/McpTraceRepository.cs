using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public class McpTraceRepository : IMcpTraceRepository
{
    private readonly IDbContextFactory<AgentsDbContext> _dbContextFactory;
    private readonly ILogger<McpTraceRepository> _logger;

    public McpTraceRepository(
        IDbContextFactory<AgentsDbContext> dbContextFactory,
        ILogger<McpTraceRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task CreateAsync(McpTraceEntity entity, CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            dbContext.McpTraces.Add(entity);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist MCP trace for method {Method}", entity.Method);
        }
    }

    public async Task<(IReadOnlyList<McpTraceEntity> Items, int TotalCount)> ListAsync(
        Guid userId, int offset, int limit, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var query = dbContext.McpTraces
            .IgnoreQueryFilters()
            .Where(e => e.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip(offset)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<McpTraceEntity?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        return await dbContext.McpTraces
            .IgnoreQueryFilters()
            .Where(e => e.Id == id && e.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }
}
