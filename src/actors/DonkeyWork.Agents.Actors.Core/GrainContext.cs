using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core;

public class GrainContext
{
    public string GrainKey { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string UserId { get; set; } = "";
    public IAgentResponseObserver? Observer { get; set; }
    public IGrainFactory GrainFactory { get; set; } = null!;
    public ILogger Logger { get; set; } = null!;
    public Action<string>? ProgressCallback { get; set; }
    public SandboxProvisioningHandle? SandboxHandle { get; set; }
    public McpServerReference[] McpServers { get; set; } = [];
    public A2aServerReference[] A2aServers { get; set; } = [];
    public SubAgentReference[] SubAgents { get; set; } = [];
    public string[] ToolGroups { get; set; } = [];
    public string? Icon { get; set; }
    public string? DisplayName { get; set; }

    public void ReportProgress(string breadcrumb) => ProgressCallback?.Invoke(breadcrumb);
}
