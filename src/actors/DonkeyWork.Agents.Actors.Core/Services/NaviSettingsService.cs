using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Actors;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Actors.Core.Services;

public sealed class NaviSettingsService : INaviSettingsService
{
    private const string CustomModelPrefix = "custom:";
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IModelCatalogService _modelCatalogService;
    private readonly ICustomModelService _customModelService;

    public NaviSettingsService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IModelCatalogService modelCatalogService,
        ICustomModelService customModelService)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _modelCatalogService = modelCatalogService;
        _customModelService = customModelService;
    }

    public async Task<NaviSettingsV1> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.NaviSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        var modelId = entity?.ModelId;
        if (modelId is null || !await ModelExistsAsync(modelId, cancellationToken))
            modelId = NaviDefaults.ModelId;

        return new NaviSettingsV1
        {
            ModelId = modelId,
        };
    }

    public async Task<NaviSettingsV1> UpdateAsync(
        UpdateNaviSettingsRequestV1 request,
        CancellationToken cancellationToken = default)
    {
        var modelId = request.ModelId.Trim();
        await ValidateModelAsync(modelId, cancellationToken);

        var entity = await _dbContext.NaviSettings.SingleOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new NaviSettingsEntity
            {
                UserId = _identityContext.UserId,
                ModelId = modelId,
            };
            _dbContext.NaviSettings.Add(entity);
        }
        else
        {
            entity.ModelId = modelId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new NaviSettingsV1 { ModelId = entity.ModelId };
    }

    private async Task ValidateModelAsync(string modelId, CancellationToken cancellationToken)
    {
        if (!await ModelExistsAsync(modelId, cancellationToken))
        {
            if (modelId.StartsWith(CustomModelPrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The selected custom model does not exist.");

            throw new ArgumentException("The selected model does not exist.");
        }
    }

    private async Task<bool> ModelExistsAsync(string modelId, CancellationToken cancellationToken)
    {
        if (!modelId.StartsWith(CustomModelPrefix, StringComparison.OrdinalIgnoreCase))
            return _modelCatalogService.GetModelById(modelId)?.Provider == LlmProvider.Anthropic;

        return Guid.TryParse(modelId[CustomModelPrefix.Length..], out var customModelId)
            && await _customModelService.ResolveAsync(customModelId, cancellationToken) is not null;
    }
}
