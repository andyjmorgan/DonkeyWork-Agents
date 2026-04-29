using Asp.Versioning;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Orchestrations.Api.Controllers;

/// <summary>
/// Manage audio collections (folders of ordered TTS recordings).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/audio-collections")]
[Authorize]
[Produces("application/json")]
public class AudioCollectionsController : ControllerBase
{
    private readonly IAudioCollectionService _service;

    public AudioCollectionsController(IAudioCollectionService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all audio collections for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ListAudioCollectionsResponseV1>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ListAsync(offset, limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific audio collection by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AudioCollectionV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Create a new audio collection.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AudioCollectionV1>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAudioCollectionRequestV1 request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1" }, result);
    }

    /// <summary>
    /// Update an existing audio collection. Omit a field to leave it unchanged.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AudioCollectionV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAudioCollectionRequestV1 request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Delete an audio collection. Recordings in the collection are preserved (become unfiled).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// List recordings in a collection, ordered by sequence number.
    /// </summary>
    [HttpGet("{id:guid}/recordings")]
    [ProducesResponseType<ListRecordingsResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListRecordings(
        Guid id,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ListRecordingsAsync(id, offset, limit, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }
}
