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
}
