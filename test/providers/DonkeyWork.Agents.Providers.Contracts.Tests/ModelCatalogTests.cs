using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Providers.Contracts.Models;
using Json.Schema;
using Xunit;

namespace DonkeyWork.Agents.Providers.Contracts.Tests;

public class ModelCatalogTests
{
    private const string ModelsResourceName = "DonkeyWork.Agents.Providers.Contracts.Data.models.json";
    private const string SchemaResourceName = "DonkeyWork.Agents.Providers.Contracts.Data.models.schema.json";

    private static readonly Assembly ContractsAssembly = typeof(ModelDefinition).Assembly;

    private static string GetEmbeddedResource(string resourceName)
    {
        using var stream = ContractsAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void ModelsJson_AdhereToSchema_ReturnsValid()
    {
        // Arrange
        var schemaJson = GetEmbeddedResource(SchemaResourceName);
        var modelsJson = GetEmbeddedResource(ModelsResourceName);

        var schema = JsonSchema.FromText(schemaJson);
        var modelsDocument = JsonDocument.Parse(modelsJson);

        // Act
        var result = schema.Evaluate(modelsDocument.RootElement);

        // Assert
        Assert.True(result.IsValid, $"Schema validation failed: {string.Join(", ", GetValidationErrors(result))}");
    }

    [Fact]
    public void ModelsJson_Deserialize_ReturnsModelCatalog()
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);

        // Act
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Assert
        Assert.NotNull(catalog);
        Assert.NotNull(catalog.Models);
        Assert.NotEmpty(catalog.Models);
    }

    [Fact]
    public void ModelsJson_Deserialize_AllModelsHaveRequiredFields()
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Act & Assert
        Assert.NotNull(catalog?.Models);

        foreach (var model in catalog.Models)
        {
            Assert.False(string.IsNullOrWhiteSpace(model.Id), "Model Id should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(model.Name), "Model Name should not be empty");
            Assert.True(Enum.IsDefined(model.Provider), $"Invalid provider for model {model.Id}");
            Assert.True(Enum.IsDefined(model.Mode), $"Invalid mode for model {model.Id}");
            Assert.True(model.MaxInputTokens >= 0, $"MaxInputTokens should be non-negative for model {model.Id}");
            Assert.True(model.MaxOutputTokens >= 0, $"MaxOutputTokens should be non-negative for model {model.Id}");
            Assert.True(model.InputCostPerMillionTokens >= 0, $"InputCostPerMillionTokens should be non-negative for model {model.Id}");
            Assert.True(model.OutputCostPerMillionTokens >= 0, $"OutputCostPerMillionTokens should be non-negative for model {model.Id}");
            Assert.NotNull(model.Supports);
            Assert.NotNull(model.ClientTypes);
            Assert.NotEmpty(model.ClientTypes);
        }
    }

    [Fact]
    public void ModelsJson_Deserialize_AllModelIdsAreUnique()
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Act
        Assert.NotNull(catalog?.Models);
        var duplicateIds = catalog.Models
            .GroupBy(m => m.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        // Assert
        Assert.Empty(duplicateIds);
    }

    [Theory]
    [InlineData("gpt-5")]
    [InlineData("gpt-5-mini")]
    [InlineData("gpt-5-nano")]
    [InlineData("gpt-image-1.5")]
    [InlineData("gpt-4o-mini-tts")]
    [InlineData("sora-2")]
    [InlineData("claude-opus-4-5")]
    [InlineData("claude-sonnet-4-5")]
    [InlineData("claude-haiku-4-5")]
    [InlineData("gemini-2.5-pro")]
    [InlineData("gemini-2.5-flash")]
    [InlineData("gemini-3-pro")]
    [InlineData("gemini-3-flash")]
    [InlineData("veo-3.1")]
    public void ModelsJson_Deserialize_SpecificModelExists(string modelId)
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Act
        Assert.NotNull(catalog?.Models);
        var model = catalog.Models.FirstOrDefault(m => m.Id == modelId);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void ModelsJson_Deserialize_SupportsFieldsArePopulated()
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Act & Assert
        Assert.NotNull(catalog?.Models);

        foreach (var model in catalog.Models)
        {
            var supports = model.Supports;
            Assert.NotNull(supports);

            // Verify boolean fields are accessible (they have default values, so just access them)
            _ = supports.Vision;
            _ = supports.AudioInput;
            _ = supports.AudioOutput;
            _ = supports.FunctionCalling;
            _ = supports.ToolChoice;
            _ = supports.PromptCaching;
            _ = supports.Reasoning;
            _ = supports.ImageOutput;
            _ = supports.Streaming;
        }
    }

    [Fact]
    public void ModelsJson_Deserialize_ClientTypesAreValid()
    {
        // Arrange
        var modelsJson = GetEmbeddedResource(ModelsResourceName);
        var catalog = JsonSerializer.Deserialize<ModelCatalog>(modelsJson);

        // Act & Assert
        Assert.NotNull(catalog?.Models);

        foreach (var model in catalog.Models)
        {
            foreach (var clientType in model.ClientTypes)
            {
                Assert.True(Enum.IsDefined(clientType), $"Invalid client type '{clientType}' for model {model.Id}");
            }
        }
    }

    private static IEnumerable<string> GetValidationErrors(EvaluationResults result)
    {
        if (!result.IsValid && result.Errors != null)
        {
            foreach (var error in result.Errors)
            {
                yield return $"{error.Key}: {error.Value}";
            }
        }

        if (result.Details != null)
        {
            foreach (var detail in result.Details)
            {
                foreach (var error in GetValidationErrors(detail))
                {
                    yield return error;
                }
            }
        }
    }
}
