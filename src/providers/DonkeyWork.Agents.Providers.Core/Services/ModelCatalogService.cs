using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Services;

namespace DonkeyWork.Agents.Providers.Core.Services;

public sealed class ModelCatalogService : IModelCatalogService
{
    private const string ModelsResourceName = "DonkeyWork.Agents.Providers.Contracts.Data.models.json";
    private static readonly Lazy<IReadOnlyList<ModelDefinition>> LazyModels = new(LoadModels);

    private static IReadOnlyList<ModelDefinition> LoadModels()
    {
        var assembly = typeof(ModelDefinition).Assembly;
        using var stream = assembly.GetManifestResourceStream(ModelsResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ModelsResourceName}' not found.");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var catalog = JsonSerializer.Deserialize<ModelCatalog>(json)
            ?? throw new InvalidOperationException("Failed to deserialize model catalog.");

        return catalog.Models;
    }

    public IReadOnlyList<ModelDefinition> GetAllModels() => LazyModels.Value;

    public ModelDefinition? GetModelById(string id) =>
        LazyModels.Value.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ModelDefinition> GetModelsByProvider(LlmProvider provider) =>
        LazyModels.Value.Where(m => m.Provider == provider).ToList();

    public IReadOnlyList<ModelDefinition> GetModelsByClientType(ProviderClientType clientType) =>
        LazyModels.Value.Where(m => m.ClientTypes.Contains(clientType)).ToList();

    public IReadOnlyList<ModelDefinition> GetModelsByMode(ModelMode mode) =>
        LazyModels.Value.Where(m => m.Mode == mode).ToList();
}
