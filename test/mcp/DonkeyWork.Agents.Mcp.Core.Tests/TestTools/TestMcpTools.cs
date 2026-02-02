using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Tests.TestTools;

/// <summary>
/// Test MCP tools for unit testing tool discovery.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class TestMcpTools
{
    /// <summary>
    /// A simple test tool that returns a greeting.
    /// </summary>
    [McpServerTool(Name = "test_greeting")]
    public string GetGreeting([Description("Name to greet")] string name)
    {
        return $"Hello, {name}!";
    }

    /// <summary>
    /// A test tool that adds two numbers.
    /// </summary>
    [McpServerTool(Name = "test_add")]
    public int Add(
        [Description("First number")] int a,
        [Description("Second number")] int b)
    {
        return a + b;
    }
}

/// <summary>
/// Another test MCP tool class for verifying multiple tool types.
/// </summary>
[McpServerToolType]
public class AnotherTestMcpTools
{
    /// <summary>
    /// A test tool that echoes input.
    /// </summary>
    [McpServerTool(Name = "test_echo")]
    public string Echo([Description("Text to echo")] string text)
    {
        return text;
    }
}

/// <summary>
/// A class without the McpServerToolType attribute - should not be discovered.
/// </summary>
public class NonMcpTools
{
    [McpServerTool(Name = "should_not_discover")]
    public string ShouldNotBeDiscovered()
    {
        return "This should not be discovered";
    }
}
