using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

public interface IToolGroupService
{
    IReadOnlyList<ToolGroupDefinitionV1> GetAll();
}
