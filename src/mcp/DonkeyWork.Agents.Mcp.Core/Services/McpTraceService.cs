using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public class McpTraceService : IMcpTraceService
{
    private readonly IMcpTraceRepository _repository;
    private readonly IIdentityContext _identityContext;

    public McpTraceService(IMcpTraceRepository repository, IIdentityContext identityContext)
    {
        _repository = repository;
        _identityContext = identityContext;
    }

    public async Task<PaginatedResponse<McpTraceSummaryV1>> ListAsync(
        int offset, int limit, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.ListAsync(
            _identityContext.UserId, offset, limit, ct);

        return new PaginatedResponse<McpTraceSummaryV1>
        {
            Items = items.Select(e => new McpTraceSummaryV1
            {
                Id = e.Id,
                Method = e.Method,
                HttpStatusCode = e.HttpStatusCode,
                IsSuccess = e.IsSuccess,
                DurationMs = e.DurationMs,
                StartedAt = e.StartedAt,
                ClientIpAddress = e.ClientIpAddress,
                UserAgent = e.UserAgent,
            }).ToList(),
            Offset = offset,
            Limit = limit,
            TotalCount = totalCount,
        };
    }

    public async Task<McpTraceDetailV1?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, _identityContext.UserId, ct);
        if (entity is null)
            return null;

        return new McpTraceDetailV1
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Method = entity.Method,
            JsonRpcId = entity.JsonRpcId,
            RequestBody = entity.RequestBody,
            ResponseBody = entity.ResponseBody,
            HttpStatusCode = entity.HttpStatusCode,
            IsSuccess = entity.IsSuccess,
            ErrorMessage = entity.ErrorMessage,
            ClientIpAddress = entity.ClientIpAddress,
            UserAgent = entity.UserAgent,
            DurationMs = entity.DurationMs,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            CreatedAt = entity.CreatedAt,
        };
    }
}
