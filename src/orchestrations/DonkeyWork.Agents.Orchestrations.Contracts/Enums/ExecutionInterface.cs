namespace DonkeyWork.Agents.Orchestrations.Contracts.Enums;

/// <summary>
/// Represents the interface through which an orchestration execution was triggered.
/// </summary>
public enum ExecutionInterface
{
    /// <summary>
    /// Direct API call (internal execution).
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Model Context Protocol (MCP) interface.
    /// </summary>
    MCP = 1,

    /// <summary>
    /// Agent-to-Agent (A2A) protocol interface.
    /// </summary>
    A2A = 2,

    /// <summary>
    /// Chat interface (conversational).
    /// </summary>
    Chat = 3,

    /// <summary>
    /// Webhook trigger interface.
    /// </summary>
    Webhook = 4
}
