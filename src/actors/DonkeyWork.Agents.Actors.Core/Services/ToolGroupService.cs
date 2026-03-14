using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Actors.Core.Tools;

namespace DonkeyWork.Agents.Actors.Core.Services;

public sealed class ToolGroupService : IToolGroupService
{
    private readonly AgentToolRegistry _toolRegistry;

    public ToolGroupService(AgentToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public IReadOnlyList<ToolGroupDefinitionV1> GetAll()
    {
        var result = new List<ToolGroupDefinitionV1>();

        foreach (var (groupId, types) in ToolGroupMap.Groups)
        {
            var displayName = ToolGroupMap.DisplayNames.TryGetValue(groupId, out var dn) ? dn : groupId;
            var tools = _toolRegistry.GetToolDefinitions(types)
                .Select(t => new ToolDefinitionV1
                {
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                })
                .ToList();

            result.Add(new ToolGroupDefinitionV1
            {
                Id = groupId,
                Name = displayName,
                Tools = tools,
            });
        }

        return result;
    }
}
