using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Scheduling.Api.Controllers;

/// <summary>
/// Manage scheduled jobs.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/schedules")]
[Authorize]
[Produces("application/json")]
public class SchedulesController : ControllerBase
{
    private readonly ISchedulingService _schedulingService;

    public SchedulesController(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    /// <summary>
    /// Create a new scheduled job.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<CreateScheduleResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequestV1 request)
    {
        try
        {
            var result = await _schedulingService.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a scheduled job by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ScheduledJobDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var detail = await _schedulingService.GetAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>
    /// List scheduled jobs for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ScheduledJobSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ScheduleJobType? jobType = null,
        [FromQuery] ScheduleTargetType? targetType = null,
        [FromQuery] ScheduleMode? scheduleMode = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] PaginationRequest? pagination = null)
    {
        var result = await _schedulingService.ListAsync(jobType, targetType, scheduleMode, isEnabled, false, pagination);
        return Ok(result);
    }

    /// <summary>
    /// Update a scheduled job.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UpdateScheduleResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduleRequestV1 request)
    {
        try
        {
            var result = await _schedulingService.UpdateAsync(id, request);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a scheduled job.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _schedulingService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Enable a scheduled job.
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enable(Guid id)
    {
        var result = await _schedulingService.EnableAsync(id);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Disable a scheduled job.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disable(Guid id)
    {
        var result = await _schedulingService.DisableAsync(id);
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Manually trigger a scheduled job to run immediately.
    /// </summary>
    [HttpPost("{id:guid}/trigger")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Trigger(Guid id)
    {
        var result = await _schedulingService.TriggerAsync(id);
        return result ? Accepted() : NotFound();
    }

    /// <summary>
    /// List execution history for a scheduled job.
    /// </summary>
    [HttpGet("{id:guid}/executions")]
    [ProducesResponseType<PaginatedResponse<ScheduledJobExecutionV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListExecutions(Guid id, [FromQuery] PaginationRequest? pagination = null)
    {
        var result = await _schedulingService.ListExecutionsAsync(id, pagination);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific execution detail.
    /// </summary>
    [HttpGet("executions/{executionId:guid}")]
    [ProducesResponseType<ScheduledJobExecutionV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecution(Guid executionId)
    {
        var execution = await _schedulingService.GetExecutionAsync(executionId);
        return execution is null ? NotFound() : Ok(execution);
    }
}
