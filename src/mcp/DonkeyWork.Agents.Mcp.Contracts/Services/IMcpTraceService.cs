using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

public interface IMcpTraceService
{
    Task<PaginatedResponse<McpTraceSummaryV1>> ListAsync(int offset, int limit, CancellationToken ct = default);

    Task<McpTraceDetailV1?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
