using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public static class TestDataBuilder
{
    #region API Key Builders

    public static CreateApiKeyRequestV1 CreateApiKeyRequest(string? name = null, string? description = null)
    {
        return new CreateApiKeyRequestV1
        {
            Name = name ?? $"test-api-key-{Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test API key description"
        };
    }

    #endregion

    #region External API Key Builders

    public static CreateExternalApiKeyRequestV1 CreateExternalApiKeyRequest(
        ExternalApiKeyProvider provider = ExternalApiKeyProvider.OpenAI,
        string? name = null,
        string? apiKey = null)
    {
        return new CreateExternalApiKeyRequestV1
        {
            Provider = provider,
            Name = name ?? $"test-{provider.ToString().ToLower()}-key-{Guid.NewGuid().ToString("N")[..8]}",
            ApiKey = apiKey ?? $"sk-test-{Guid.NewGuid():N}"
        };
    }

    #endregion

    #region MCP Server Builders

    public static CreateMcpServerRequestV1 CreateMcpStdioServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? command = null,
        List<string>? arguments = null)
    {
        return new CreateMcpServerRequestV1
        {
            Name = name ?? $"Test MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test MCP server description",
            TransportType = McpTransportType.Stdio,
            IsEnabled = isEnabled,
            StdioConfiguration = new CreateMcpStdioConfigurationRequestV1
            {
                Command = command ?? "npx",
                Arguments = arguments ?? ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"]
            }
        };
    }

    public static CreateMcpServerRequestV1 CreateMcpHttpServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? endpoint = null,
        McpHttpTransportMode transportMode = McpHttpTransportMode.AutoDetect,
        McpHttpAuthType authType = McpHttpAuthType.None)
    {
        return new CreateMcpServerRequestV1
        {
            Name = name ?? $"Test MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Test MCP server description",
            TransportType = McpTransportType.Http,
            IsEnabled = isEnabled,
            HttpConfiguration = new CreateMcpHttpConfigurationRequestV1
            {
                Endpoint = endpoint ?? "https://example.com/mcp",
                TransportMode = transportMode,
                AuthType = authType
            }
        };
    }

    public static UpdateMcpServerRequestV1 UpdateMcpStdioServerRequest(
        string? name = null,
        string? description = null,
        bool isEnabled = true,
        string? command = null)
    {
        return new UpdateMcpServerRequestV1
        {
            Name = name ?? $"Updated MCP Server {Guid.NewGuid().ToString("N")[..8]}",
            Description = description ?? "Updated MCP server description",
            TransportType = McpTransportType.Stdio,
            IsEnabled = isEnabled,
            StdioConfiguration = new CreateMcpStdioConfigurationRequestV1
            {
                Command = command ?? "python"
            }
        };
    }

    #endregion
}
