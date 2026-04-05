using DonkeyWork.Agents.Persistence.Entities.Mcp;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public interface IMcpTraceRepository
{
    Task CreateAsync(McpTraceEntity entity, CancellationToken ct = default);

    Task<(IReadOnlyList<McpTraceEntity> Items, int TotalCount)> ListAsync(
        Guid userId, int offset, int limit, CancellationToken ct = default);

    Task<McpTraceEntity?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
}
