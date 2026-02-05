using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// Authentication type for HTTP MCP server connections.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum McpHttpAuthType
{
    /// <summary>
    /// No authentication required.
    /// </summary>
    None = 0,

    /// <summary>
    /// OAuth 2.1 authentication.
    /// </summary>
    OAuth = 1,

    /// <summary>
    /// Header-based authentication (API keys, bearer tokens, etc.).
    /// </summary>
    Header = 2
}
