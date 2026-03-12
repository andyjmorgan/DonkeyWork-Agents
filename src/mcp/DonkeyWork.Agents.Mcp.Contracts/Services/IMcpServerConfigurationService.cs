using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Mcp.Contracts.Services;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public interface IMcpServerConfigurationService
{
    /// <summary>
    /// Creates a new MCP server configuration.
    /// </summary>
    Task<McpServerDetailsV1> CreateAsync(CreateMcpServerRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an MCP server configuration by ID.
    /// </summary>
    Task<McpServerDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all MCP server configurations for the current user.
    /// </summary>
    Task<IReadOnlyList<McpServerSummaryV1>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an MCP server configuration.
    /// </summary>
    Task<McpServerDetailsV1?> UpdateAsync(Guid id, UpdateMcpServerRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an MCP server configuration.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection-ready configurations for all enabled HTTP MCP servers,
    /// with decrypted header values for authentication.
    /// </summary>
    Task<IReadOnlyList<McpConnectionConfigV1>> GetEnabledConnectionConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection-ready configurations for all enabled stdio MCP servers.
    /// </summary>
    Task<IReadOnlyList<McpStdioConnectionConfigV1>> GetEnabledStdioConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection-ready configurations for all enabled HTTP MCP servers that have ConnectToNavi set.
    /// </summary>
    Task<IReadOnlyList<McpConnectionConfigV1>> GetNaviConnectionConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets connection-ready configurations for all enabled stdio MCP servers that have ConnectToNavi set.
    /// </summary>
    Task<IReadOnlyList<McpStdioConnectionConfigV1>> GetNaviStdioConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a connection-ready configuration for a specific MCP server by ID,
    /// with decrypted header values for authentication.
    /// Returns null if the server is not found or is not an HTTP server.
    /// </summary>
    Task<McpConnectionConfigV1?> GetConnectionConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
