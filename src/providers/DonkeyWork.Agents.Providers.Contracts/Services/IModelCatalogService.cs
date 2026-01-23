using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models;

namespace DonkeyWork.Agents.Providers.Contracts.Services;

public interface IModelCatalogService
{
    IReadOnlyList<ModelDefinition> GetAllModels();

    ModelDefinition? GetModelById(string id);

    IReadOnlyList<ModelDefinition> GetModelsByProvider(LlmProvider provider);

    IReadOnlyList<ModelDefinition> GetModelsByClientType(ProviderClientType clientType);

    IReadOnlyList<ModelDefinition> GetModelsByMode(ModelMode mode);
}
