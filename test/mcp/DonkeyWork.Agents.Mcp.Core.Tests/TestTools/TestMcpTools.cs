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
    [McpTool(
        Name = "test_greeting",
        Title = "Get Greeting",
        Description = "Returns a greeting for the given name",
        Icon = "wave",
        ReadOnlyHint = true)]
    public string GetGreeting([Description("Name to greet")] string name)
    {
        return $"Hello, {name}!";
    }

    /// <summary>
    /// A test tool that adds two numbers.
    /// </summary>
    [McpServerTool(Name = "test_add")]
    [McpTool(
        Name = "test_add",
        Title = "Add Numbers",
        Description = "Adds two numbers together",
        Icon = "calculator",
        ReadOnlyHint = true,
        IdempotentHint = true)]
    public int Add(
        [Description("First number")] int a,
        [Description("Second number")] int b)
    {
        return a + b;
    }

    /// <summary>
    /// A test tool that simulates a destructive operation.
    /// </summary>
    [McpServerTool(Name = "test_delete")]
    [McpTool(
        Name = "test_delete",
        Title = "Delete Item",
        Description = "Simulates deleting an item",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public bool Delete([Description("Item ID to delete")] string id)
    {
        return true;
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
    [McpTool(
        Name = "test_echo",
        Title = "Echo",
        Description = "Echoes the input text",
        Icon = "repeat",
        ReadOnlyHint = true,
        IdempotentHint = true)]
    public string Echo([Description("Text to echo")] string text)
    {
        return text;
    }

    /// <summary>
    /// A test tool that simulates an external API call.
    /// </summary>
    [McpServerTool(Name = "test_external_api")]
    [McpTool(
        Name = "test_external_api",
        Title = "Call External API",
        Description = "Simulates calling an external API",
        Icon = "globe",
        OpenWorldHint = true,
        ReadOnlyHint = false)]
    public string CallExternalApi([Description("API endpoint")] string endpoint)
    {
        return $"Called {endpoint}";
    }
}

/// <summary>
/// A class without the McpServerToolType attribute - should not be discovered.
/// </summary>
public class NonMcpTools
{
    [McpServerTool(Name = "should_not_discover")]
    [McpTool(
        Name = "should_not_discover",
        Title = "Should Not Discover",
        Description = "This should not be discovered")]
    public string ShouldNotBeDiscovered()
    {
        return "This should not be discovered";
    }
}
