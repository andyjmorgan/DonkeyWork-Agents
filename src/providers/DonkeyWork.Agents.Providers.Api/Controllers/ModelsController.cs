using Asp.Versioning;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Models.Api;
using DonkeyWork.Agents.Providers.Contracts.Models.Schema;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Providers.Api.Controllers;

/// <summary>
/// Endpoints for retrieving available models and their configuration schemas.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class ModelsController : ControllerBase
{
    private readonly IModelCatalogService _modelCatalogService;
    private readonly IModelConfigSchemaService _modelConfigSchemaService;

    public ModelsController(
        IModelCatalogService modelCatalogService,
        IModelConfigSchemaService modelConfigSchemaService)
    {
        _modelCatalogService = modelCatalogService;
        _modelConfigSchemaService = modelConfigSchemaService;
    }

    /// <summary>
    /// Gets all available models.
    /// </summary>
    /// <returns>A list of all available models.</returns>
    /// <response code="200">Returns the list of models.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    [ProducesResponseType<GetModelsResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetModels()
    {
        var models = _modelCatalogService.GetAllModels();
        return Ok(new GetModelsResponseV1 { Models = models });
    }

    /// <summary>
    /// Gets a specific model by its ID.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The model definition.</returns>
    /// <response code="200">Returns the model definition.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the model is not found.</response>
    [HttpGet("{modelId}")]
    [ProducesResponseType<ModelDefinition>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetModel(string modelId)
    {
        var model = _modelCatalogService.GetModelById(modelId);
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    /// <summary>
    /// Gets the configuration schema for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The model's configuration schema.</returns>
    /// <response code="200">Returns the model's configuration schema.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the model is not found or has no configuration schema.</response>
    [HttpGet("{modelId}/config-schema")]
    [ProducesResponseType<ModelConfigSchema>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetConfigSchema(string modelId)
    {
        var schema = _modelConfigSchemaService.GetSchemaForModel(modelId);
        if (schema == null)
        {
            return NotFound();
        }

        return Ok(schema);
    }

    /// <summary>
    /// Gets configuration schemas for all available models.
    /// </summary>
    /// <returns>Configuration schemas for all models.</returns>
    /// <response code="200">Returns all configuration schemas.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("config-schemas")]
    [ProducesResponseType<GetConfigSchemasResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetAllConfigSchemas()
    {
        var schemas = _modelConfigSchemaService.GetAllSchemas();
        return Ok(new GetConfigSchemasResponseV1 { Schemas = schemas });
    }
}
