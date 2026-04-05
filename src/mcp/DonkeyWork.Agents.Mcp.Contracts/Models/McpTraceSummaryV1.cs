namespace DonkeyWork.Agents.Mcp.Contracts.Models;

public class McpTraceSummaryV1
{
    public Guid Id { get; init; }

    public string Method { get; init; } = string.Empty;

    public int HttpStatusCode { get; init; }

    public bool IsSuccess { get; init; }

    public int? DurationMs { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public string? ClientIpAddress { get; init; }

    public string? UserAgent { get; init; }
}
