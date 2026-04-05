namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Immutable audit log for all MCP JSON-RPC traffic.
/// Does not inherit from BaseEntity as this is system-level logging.
/// </summary>
public class McpTraceEntity
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string Method { get; init; } = string.Empty;

    public string? JsonRpcId { get; init; }

    public string RequestBody { get; init; } = string.Empty;

    public string? ResponseBody { get; init; }

    public int HttpStatusCode { get; init; }

    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ClientIpAddress { get; init; }

    public string? UserAgent { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public int? DurationMs { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
