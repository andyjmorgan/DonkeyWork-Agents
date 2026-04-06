using System.ComponentModel;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Identity.Api.McpTools;

/// <summary>
/// MCP tools for identity operations.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class IdentityTools
{
    private readonly IIdentityContext _identityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityTools"/> class.
    /// </summary>
    public IdentityTools(IIdentityContext identityContext)
    {
        _identityContext = identityContext;
    }

    /// <summary>
    /// Returns the current authenticated user's ID.
    /// </summary>
    [McpServerTool(Name = "identity_whoami", Title = "Who Am I", ReadOnly = true)]
    [Description("Returns the current authenticated user's unique identifier (user ID). Use this to identify the current user context.")]
    public string WhoAmI()
    {
        return _identityContext.UserId.ToString();
    }
}
