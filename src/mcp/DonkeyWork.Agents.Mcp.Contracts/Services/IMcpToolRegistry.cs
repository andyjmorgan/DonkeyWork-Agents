using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

/// <summary>
/// Singleton registry that holds all discovered MCP tools.
/// Tools are discovered once at startup and cached for the lifetime of the application.
/// </summary>
public interface IMcpToolRegistry
{
    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>A read-only collection of all registered tools.</returns>
    IReadOnlyList<McpServerTool> GetAllTools();

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="name">The name of the tool to retrieve.</param>
    /// <returns>The tool if found; otherwise, null.</returns>
    McpServerTool? GetTool(string name);
}
