using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

public interface IMcpToolDiscoveryService
{
    IReadOnlyList<McpServerTool> DiscoverTools();

    McpServerTool? GetTool(string name);
}
