using Asp.Versioning;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Agents.Api.Controllers;

/// <summary>
/// Endpoints for multimodal chat model configuration.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/multimodal-chat")]
[Authorize]
[Produces("application/json")]
public class MultimodalChatController : ControllerBase
{
    private readonly IMultimodalChatSchemaService _schemaService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultimodalChatController"/> class.
    /// </summary>
    public MultimodalChatController(IMultimodalChatSchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    /// <summary>
    /// Get the configuration schema for a multimodal chat model provider.
    /// </summary>
    /// <param name="provider">The LLM provider to get schema for (OpenAI, Anthropic, Google).</param>
    /// <returns>The configuration schema for the specified provider.</returns>
    /// <response code="200">Returns the configuration schema.</response>
    /// <response code="400">Invalid provider specified.</response>
    [HttpGet("schema")]
    [ProducesResponseType<GetMultimodalChatSchemaResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetSchema([FromQuery] LlmProvider provider)
    {
        if (provider == LlmProvider.Unknown)
        {
            return BadRequest(new { error = "Invalid provider. Must be one of: OpenAI, Anthropic, Google" });
        }

        var schema = _schemaService.GenerateSchema(provider);

        return Ok(new GetMultimodalChatSchemaResponseV1
        {
            Schema = schema
        });
    }
}
