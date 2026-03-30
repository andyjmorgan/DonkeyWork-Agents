using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

public interface IA2aMcpToolService
{
    Task<IReadOnlyList<Tool>> DiscoverToolsAsync(CancellationToken cancellationToken = default);

    bool CanHandle(string toolName);

    Task<CallToolResult> ExecuteAsync(string toolName, JsonElement? arguments, CancellationToken cancellationToken = default);
}
