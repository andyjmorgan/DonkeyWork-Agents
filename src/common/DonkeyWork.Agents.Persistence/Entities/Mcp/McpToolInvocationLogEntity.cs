namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Immutable audit log for MCP tool invocations.
/// Does not inherit from BaseEntity as this is system-level logging, not user-owned data.
/// </summary>
public class McpToolInvocationLogEntity
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User who made the call. Nullable for anonymous/system calls.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Name of the tool that was invoked.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Provider of the tool (e.g., DonkeyWork, Microsoft, Google).
    /// </summary>
    public string? ToolProvider { get; init; }

    /// <summary>
    /// JSON representation of the request parameters.
    /// </summary>
    public string RequestParams { get; init; } = string.Empty;

    /// <summary>
    /// JSON representation of the response. Null if failed before response.
    /// </summary>
    public string? ResponseContent { get; init; }

    /// <summary>
    /// Whether the invocation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the invocation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the invocation was initiated.
    /// </summary>
    public DateTimeOffset InvokedAt { get; init; }

    /// <summary>
    /// When the invocation completed. Null if not yet completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Duration of the invocation in milliseconds.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// MCP client identifier.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? ClientIpAddress { get; init; }

    /// <summary>
    /// User agent string of the client.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// MCP session ID if available.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// When the log entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
