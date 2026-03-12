using System.Text.Json;
using Anthropic.Models.Beta.Messages;

namespace DonkeyWork.Agents.Actors.Core.Providers.Anthropic;

internal static class AnthropicToolMapper
{
    public static List<BetaToolUnion>? MapTools(
        IReadOnlyList<InternalToolDefinition>? tools, ProviderOptions options)
    {
        var hasClientTools = tools is { Count: > 0 };
        var hasServerTools = options.WebSearch.Enabled || options.WebFetch.Enabled;

        if (!hasClientTools && !hasServerTools)
            return null;

        var result = new List<BetaToolUnion>();
        var hasDeferredTools = false;

        // Map client tools
        if (hasClientTools)
        {
            foreach (var tool in tools!)
            {
                var inputSchema = MapInputSchema(tool.InputSchema);
                result.Add(new BetaTool
                {
                    Name = tool.Name,
                    Description = tool.Description ?? string.Empty,
                    InputSchema = inputSchema,
                    DeferLoading = tool.DeferLoading ? true : null,
                });

                if (tool.DeferLoading)
                    hasDeferredTools = true;
            }
        }

        // Add tool search when there are deferred tools
        if (hasDeferredTools)
        {
            result.Insert(0, new BetaToolSearchToolBm25_20251119
            {
                Type = "tool_search_tool_bm25_20251119",
            });
        }

        // Append server tools
        if (options.WebSearch.Enabled)
        {
            result.Add(new BetaWebSearchTool20250305
            {
                MaxUses = options.WebSearch.MaxUses
            });
        }

        if (options.WebFetch.Enabled)
        {
            result.Add(new BetaWebFetchTool20250910
            {
                MaxUses = options.WebFetch.MaxUses
            });
        }

        return result.Count > 0 ? result : null;
    }

    private static InputSchema MapInputSchema(object? schema)
    {
        if (schema is null)
        {
            return InputSchema.FromRawUnchecked(
                new Dictionary<string, JsonElement>
                {
                    ["type"] = JsonSerializer.Deserialize<JsonElement>("\"object\""),
                    ["properties"] = JsonSerializer.Deserialize<JsonElement>("{}")
                });
        }

        var json = JsonSerializer.Serialize(schema);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? new Dictionary<string, JsonElement>();

        return InputSchema.FromRawUnchecked(dict);
    }
}
