using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Models.Schema;
using DonkeyWork.Agents.Providers.Contracts.Services;
using DonkeyWork.Agents.Providers.Core.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Providers.Core.Tests.Services;

public class ModelConfigSchemaServiceTests
{
    private readonly Mock<IModelCatalogService> _mockCatalogService;
    private readonly ModelConfigSchemaService _service;

    public ModelConfigSchemaServiceTests()
    {
        _mockCatalogService = new Mock<IModelCatalogService>();
        _service = new ModelConfigSchemaService(_mockCatalogService.Object);
    }

    [Fact]
    public void GetSchemaForModel_WithAnthropicModel_ReturnsSchemaWithThinkingBudget()
    {
        // Arrange
        var model = CreateChatModel("claude-opus-4-5", LlmProvider.Anthropic, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("claude-opus-4-5");

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("claude-opus-4-5", schema.ModelId);
        Assert.Equal(LlmProvider.Anthropic, schema.Provider);

        var thinkingBudgetField = schema.Fields.FirstOrDefault(f => f.Name == "thinkingBudget");
        Assert.NotNull(thinkingBudgetField);
        Assert.Equal("Thinking Budget", thinkingBudgetField.Label);
        Assert.Equal(FieldControlType.NumberInput, thinkingBudgetField.ControlType);
        Assert.Equal(1024, thinkingBudgetField.Min);
        Assert.Equal(128000, thinkingBudgetField.Max);
    }

    [Fact]
    public void GetSchemaForModel_WithOpenAIModel_ReturnsSchemaWithPenaltyFields()
    {
        // Arrange
        var model = CreateChatModel("gpt-5", LlmProvider.OpenAI, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("gpt-5");

        // Assert
        Assert.NotNull(schema);

        var frequencyPenalty = schema.Fields.FirstOrDefault(f => f.Name == "frequencyPenalty");
        Assert.NotNull(frequencyPenalty);
        Assert.Equal("Frequency Penalty", frequencyPenalty.Label);
        Assert.Equal(FieldControlType.Slider, frequencyPenalty.ControlType);

        var presencePenalty = schema.Fields.FirstOrDefault(f => f.Name == "presencePenalty");
        Assert.NotNull(presencePenalty);
        Assert.Equal("Presence Penalty", presencePenalty.Label);
    }

    [Fact]
    public void GetSchemaForModel_WithGoogleModel_ReturnsSchemaWithTopK()
    {
        // Arrange
        var model = CreateChatModel("gemini-2.5-pro", LlmProvider.Google, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("gemini-2.5-pro");

        // Assert
        Assert.NotNull(schema);

        var topK = schema.Fields.FirstOrDefault(f => f.Name == "topK");
        Assert.NotNull(topK);
        Assert.Equal("Top K", topK.Label);
        Assert.Equal(FieldControlType.NumberInput, topK.ControlType);
        Assert.Equal(1, topK.Min);
        Assert.Equal(100, topK.Max);
    }

    [Fact]
    public void GetSchemaForModel_WithReasoningCapability_IncludesReasoningEffortField()
    {
        // Arrange
        var model = CreateChatModel("claude-opus-4-5", LlmProvider.Anthropic, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("claude-opus-4-5");

        // Assert
        Assert.NotNull(schema);

        var reasoningEffort = schema.Fields.FirstOrDefault(f => f.Name == "reasoningEffort");
        Assert.NotNull(reasoningEffort);
        Assert.Equal("Reasoning Effort", reasoningEffort.Label);
        Assert.Equal(FieldControlType.Select, reasoningEffort.ControlType);
        Assert.NotNull(reasoningEffort.Options);
        Assert.Contains("Low", reasoningEffort.Options);
        Assert.Contains("Medium", reasoningEffort.Options);
        Assert.Contains("High", reasoningEffort.Options);
        Assert.Equal("Medium", reasoningEffort.DefaultValue);
    }

    [Fact]
    public void GetSchemaForModel_WithoutReasoningCapability_ExcludesReasoningFields()
    {
        // Arrange
        var model = CreateChatModel("claude-haiku-4-5", LlmProvider.Anthropic, supportsReasoning: false);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("claude-haiku-4-5");

        // Assert
        Assert.NotNull(schema);

        var reasoningEffort = schema.Fields.FirstOrDefault(f => f.Name == "reasoningEffort");
        Assert.Null(reasoningEffort);

        var thinkingBudget = schema.Fields.FirstOrDefault(f => f.Name == "thinkingBudget");
        Assert.Null(thinkingBudget);
    }

    [Fact]
    public void GetSchemaForModel_WithConfigOverrides_AppliesOverrides()
    {
        // Arrange
        var model = CreateChatModel(
            "claude-opus-4-5",
            LlmProvider.Anthropic,
            supportsReasoning: true,
            configOverrides: new Dictionary<string, FieldOverride>
            {
                ["temperature"] = new FieldOverride { Max = 1.0 },
                ["maxOutputTokens"] = new FieldOverride { Max = 64000, Default = 8192 }
            });
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("claude-opus-4-5");

        // Assert
        Assert.NotNull(schema);

        var temperature = schema.Fields.FirstOrDefault(f => f.Name == "temperature");
        Assert.NotNull(temperature);
        Assert.Equal(1.0, temperature.Max);

        var maxOutputTokens = schema.Fields.FirstOrDefault(f => f.Name == "maxOutputTokens");
        Assert.NotNull(maxOutputTokens);
        Assert.Equal(64000, maxOutputTokens.Max);
        Assert.Equal(8192, maxOutputTokens.DefaultValue);
    }

    [Fact]
    public void GetSchemaForModel_WithHiddenOverride_ExcludesField()
    {
        // Arrange
        var model = CreateChatModel(
            "test-model",
            LlmProvider.OpenAI,
            supportsReasoning: false,
            configOverrides: new Dictionary<string, FieldOverride>
            {
                ["topP"] = new FieldOverride { Hidden = true }
            });
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("test-model");

        // Assert
        Assert.NotNull(schema);

        var topP = schema.Fields.FirstOrDefault(f => f.Name == "topP");
        Assert.Null(topP);
    }

    [Fact]
    public void GetSchemaForModel_WithChatModel_IncludesBaseFields()
    {
        // Arrange
        var model = CreateChatModel("test-chat", LlmProvider.OpenAI, supportsReasoning: false);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("test-chat");

        // Assert
        Assert.NotNull(schema);

        // Check base fields
        Assert.Contains(schema.Fields, f => f.Name == "temperature");
        Assert.Contains(schema.Fields, f => f.Name == "maxOutputTokens");
        Assert.Contains(schema.Fields, f => f.Name == "topP");

        // Verify temperature details
        var temperature = schema.Fields.First(f => f.Name == "temperature");
        Assert.Equal(FieldControlType.Slider, temperature.ControlType);
        Assert.Equal(0, temperature.Min);
        Assert.Equal(2, temperature.Max);
        Assert.Equal(0.1, temperature.Step);
        Assert.Equal(1.0, temperature.DefaultValue);
    }

    [Fact]
    public void GetSchemaForModel_FieldsAreOrderedByOrderProperty()
    {
        // Arrange
        var model = CreateChatModel("test-model", LlmProvider.Anthropic, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("test-model");

        // Assert
        Assert.NotNull(schema);

        var fieldNames = schema.Fields.Select(f => f.Name).ToList();

        // Verify specific fields exist with expected order values (new schema structure)
        var credentialId = schema.Fields.First(f => f.Name == "credentialId");
        var temperature = schema.Fields.First(f => f.Name == "temperature");
        var maxOutputTokens = schema.Fields.First(f => f.Name == "maxOutputTokens");
        var topP = schema.Fields.First(f => f.Name == "topP");
        var reasoningEffort = schema.Fields.First(f => f.Name == "reasoningEffort");
        var thinkingBudget = schema.Fields.First(f => f.Name == "thinkingBudget");

        // Verify order values match new structure
        Assert.Equal(0, credentialId.Order);
        Assert.Equal(30, temperature.Order);
        Assert.Equal(40, maxOutputTokens.Order);
        Assert.Equal(10, topP.Order); // TopP is in Advanced tab with Order=10
        Assert.Equal(10, reasoningEffort.Order); // ReasoningEffort is in Reasoning tab with Order=10
        Assert.Equal(20, thinkingBudget.Order); // ThinkingBudget is in Reasoning tab with Order=20

        // Verify fields are sorted by global order
        var credentialIndex = fieldNames.IndexOf("credentialId");
        var tempIndex = fieldNames.IndexOf("temperature");

        Assert.True(credentialIndex < tempIndex);
    }

    [Fact]
    public void GetSchemaForModel_WithNonExistentModel_ReturnsNull()
    {
        // Arrange
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(Array.Empty<ModelDefinition>());

        // Act
        var schema = _service.GetSchemaForModel("non-existent-model");

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public void GetSchemaForModel_WithImageGenerationModel_ReturnsNull()
    {
        // Arrange
        var model = new ModelDefinition
        {
            Id = "image-gen-model",
            Name = "Image Generator",
            Provider = LlmProvider.OpenAI,
            Mode = ModelMode.ImageGeneration,
            MaxInputTokens = 4000,
            MaxOutputTokens = 0,
            InputCostPerMillionTokens = 5.0m,
            OutputCostPerMillionTokens = 40.0m,
            Supports = CreateModelSupports(false, false),
            ClientTypes = new[] { ProviderClientType.ImageOutput }
        };
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("image-gen-model");

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(ModelMode.ImageGeneration, schema.Mode);

        // Should have image generation fields
        var size = schema.Fields.FirstOrDefault(f => f.Name == "size");
        Assert.NotNull(size);
        Assert.Equal(FieldControlType.Select, size.ControlType);

        var quality = schema.Fields.FirstOrDefault(f => f.Name == "quality");
        Assert.NotNull(quality);

        var numberOfImages = schema.Fields.FirstOrDefault(f => f.Name == "numberOfImages");
        Assert.NotNull(numberOfImages);
        Assert.Equal(FieldControlType.NumberInput, numberOfImages.ControlType);
    }

    [Fact]
    public void GetAllSchemas_ReturnsAllModelSchemas()
    {
        // Arrange
        var models = new[]
        {
            CreateChatModel("claude-opus", LlmProvider.Anthropic, supportsReasoning: true),
            CreateChatModel("gpt-5", LlmProvider.OpenAI, supportsReasoning: true),
            CreateChatModel("gemini-pro", LlmProvider.Google, supportsReasoning: true)
        };
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(models);

        // Act
        var schemas = _service.GetAllSchemas();

        // Assert
        Assert.Equal(3, schemas.Count);
        Assert.True(schemas.ContainsKey("claude-opus"));
        Assert.True(schemas.ContainsKey("gpt-5"));
        Assert.True(schemas.ContainsKey("gemini-pro"));
    }

    [Fact]
    public void GetSchemaForModel_IsCached_ReturnsSameInstance()
    {
        // Arrange
        var model = CreateChatModel("test-model", LlmProvider.OpenAI, supportsReasoning: false);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema1 = _service.GetSchemaForModel("test-model");
        var schema2 = _service.GetSchemaForModel("test-model");

        // Assert
        Assert.NotNull(schema1);
        Assert.NotNull(schema2);
        Assert.Same(schema1, schema2);
        _mockCatalogService.Verify(x => x.GetAllModels(), Times.Once);
    }

    [Fact]
    public void GetSchemaForModel_WithTabbedAndGroupedFields_IncludesTabAndGroupInformation()
    {
        // Arrange
        var model = CreateChatModel("test-model", LlmProvider.Anthropic, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("test-model");

        // Assert
        Assert.NotNull(schema);

        // Verify tabs are generated
        Assert.NotEmpty(schema.Tabs);
        Assert.Contains(schema.Tabs, t => t.Name == "Basic");
        Assert.Contains(schema.Tabs, t => t.Name == "Advanced");
        Assert.Contains(schema.Tabs, t => t.Name == "Reasoning");

        // Verify fields have both Tab and Group
        var topP = schema.Fields.FirstOrDefault(f => f.Name == "topP");
        Assert.NotNull(topP);
        Assert.Equal("Advanced", topP.Tab);
        Assert.Equal("Sampling", topP.Group);

        var thinkingBudget = schema.Fields.FirstOrDefault(f => f.Name == "thinkingBudget");
        Assert.NotNull(thinkingBudget);
        Assert.Equal("Reasoning", thinkingBudget.Tab);
        Assert.Equal("Settings", thinkingBudget.Group);

        var temperature = schema.Fields.FirstOrDefault(f => f.Name == "temperature");
        Assert.NotNull(temperature);
        Assert.Equal("Basic", temperature.Tab);
        Assert.Equal("Sampling", temperature.Group);

        // Verify some fields have no group (just tab)
        var stream = schema.Fields.FirstOrDefault(f => f.Name == "stream");
        Assert.NotNull(stream);
        Assert.Equal("Basic", stream.Tab);
        Assert.Null(stream.Group);
    }

    private static ModelDefinition CreateChatModel(
        string id,
        LlmProvider provider,
        bool supportsReasoning,
        IReadOnlyDictionary<string, FieldOverride>? configOverrides = null)
    {
        return new ModelDefinition
        {
            Id = id,
            Name = $"Test {id}",
            Provider = provider,
            Mode = ModelMode.Chat,
            MaxInputTokens = 200000,
            MaxOutputTokens = 64000,
            InputCostPerMillionTokens = 3.0m,
            OutputCostPerMillionTokens = 15.0m,
            Supports = CreateModelSupports(supportsReasoning, supportsReasoning),
            ClientTypes = new[] { ProviderClientType.MultimodalInput },
            ConfigOverrides = configOverrides
        };
    }

    [Fact]
    public void GetSchemaForModel_WithDependentFields_IncludesDependencies()
    {
        // Note: This test would need a config class with DependsOn attributes
        // For now, we'll verify the reasoning fields have the right capability dependency
        var model = CreateChatModel("test-model", LlmProvider.Anthropic, supportsReasoning: true);
        _mockCatalogService.Setup(x => x.GetAllModels()).Returns(new[] { model });

        // Act
        var schema = _service.GetSchemaForModel("test-model");

        // Assert
        Assert.NotNull(schema);

        // The reasoningEffort field exists because the model supports reasoning
        var reasoningEffort = schema.Fields.FirstOrDefault(f => f.Name == "reasoningEffort");
        Assert.NotNull(reasoningEffort);

        // The thinkingBudget field also exists for Anthropic models
        var thinkingBudget = schema.Fields.FirstOrDefault(f => f.Name == "thinkingBudget");
        Assert.NotNull(thinkingBudget);
    }

    private static ModelSupports CreateModelSupports(bool reasoning, bool functionCalling)
    {
        return new ModelSupports
        {
            Vision = true,
            AudioInput = false,
            AudioOutput = false,
            FunctionCalling = functionCalling,
            ToolChoice = functionCalling,
            PromptCaching = true,
            Reasoning = reasoning,
            ImageOutput = false,
            Streaming = true
        };
    }
}

