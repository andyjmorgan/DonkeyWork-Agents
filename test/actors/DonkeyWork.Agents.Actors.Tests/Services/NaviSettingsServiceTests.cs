using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Services;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Services;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Services;

public sealed class NaviSettingsServiceTests : IDisposable
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly AgentsDbContext _dbContext;
    private readonly Mock<IIdentityContext> _identityContext = new();
    private readonly Mock<IModelCatalogService> _modelCatalogService = new();
    private readonly Mock<ICustomModelService> _customModelService = new();
    private readonly NaviSettingsService _service;

    public NaviSettingsServiceTests()
    {
        _identityContext.SetupGet(context => context.UserId).Returns(_userId);

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AgentsDbContext(options, _identityContext.Object);

        _modelCatalogService
            .Setup(service => service.GetModelById(NaviDefaults.ModelId))
            .Returns(Model(NaviDefaults.ModelId, "Default"));

        _service = new NaviSettingsService(
            _dbContext,
            _identityContext.Object,
            _modelCatalogService.Object,
            _customModelService.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetAsync_WithoutSavedSettings_ReturnsDefaultModel()
    {
        var result = await _service.GetAsync();

        Assert.Equal(NaviDefaults.ModelId, result.ModelId);
    }

    [Fact]
    public async Task UpdateAsync_WithCatalogModel_PersistsSelection()
    {
        const string modelId = "catalog-model";
        _modelCatalogService.Setup(service => service.GetModelById(modelId))
            .Returns(Model(modelId, "Catalog model"));

        await _service.UpdateAsync(new UpdateNaviSettingsRequestV1 { ModelId = modelId });
        var result = await _service.GetAsync();

        Assert.Equal(modelId, result.ModelId);
        var entity = await _dbContext.NaviSettings.SingleAsync();
        Assert.Equal(_userId, entity.UserId);
    }

    [Fact]
    public async Task UpdateAsync_WithOwnedCustomModel_PersistsSelection()
    {
        var customModelId = Guid.NewGuid();
        var catalogId = $"custom:{customModelId:D}";
        _customModelService.Setup(service => service.ResolveAsync(customModelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedCustomModel
            {
                Id = customModelId,
                Name = "Custom",
                Endpoint = "https://example.test/v1/messages",
                ModelName = "custom-model",
                WireFormat = CustomModelWireFormat.AnthropicMessages,
            });

        var result = await _service.UpdateAsync(new UpdateNaviSettingsRequestV1 { ModelId = catalogId });

        Assert.Equal(catalogId, result.ModelId);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingCustomModel_RejectsSelection()
    {
        var request = new UpdateNaviSettingsRequestV1 { ModelId = $"custom:{Guid.NewGuid():D}" };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(request));

        Assert.Equal("The selected custom model does not exist.", exception.Message);
        Assert.Empty(_dbContext.NaviSettings);
    }

    [Fact]
    public async Task GetAsync_WhenSelectedCustomModelWasDeleted_FallsBackToDefault()
    {
        var customModelId = Guid.NewGuid();
        _dbContext.NaviSettings.Add(new DonkeyWork.Agents.Persistence.Entities.Actors.NaviSettingsEntity
        {
            UserId = _userId,
            ModelId = $"custom:{customModelId:D}",
        });
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAsync();

        Assert.Equal(NaviDefaults.ModelId, result.ModelId);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownCatalogModel_RejectsSelection()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateAsync(new UpdateNaviSettingsRequestV1 { ModelId = "unknown-model" }));

        Assert.Equal("The selected model does not exist.", exception.Message);
        Assert.Empty(_dbContext.NaviSettings);
    }

    [Fact]
    public async Task UpdateAsync_WithUnsupportedBuiltInProvider_RejectsSelection()
    {
        const string modelId = "openai-model";
        _modelCatalogService.Setup(service => service.GetModelById(modelId))
            .Returns(Model(modelId, "OpenAI model", LlmProvider.OpenAI));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateAsync(new UpdateNaviSettingsRequestV1 { ModelId = modelId }));
    }

    private static ModelDefinition Model(
        string id,
        string name,
        LlmProvider provider = LlmProvider.Anthropic) => new()
    {
        Id = id,
        Name = name,
        Provider = provider,
        Mode = ModelMode.Chat,
        MaxInputTokens = 200_000,
        MaxOutputTokens = 20_000,
        InputCostPerMillionTokens = 0,
        OutputCostPerMillionTokens = 0,
        Supports = new ModelSupports(),
        ClientTypes = [],
    };
}
