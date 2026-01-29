using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Actions.Contracts.Types;

namespace DonkeyWork.Agents.Actions.Core.Providers;

/// <summary>
/// Parameters for Sleep action
/// </summary>
[ActionNode(
    actionType: "sleep",
    category: "Utilities",
    Group = "Flow Control",
    Icon = "clock",
    Description = "Pause execution for a specified duration",
    DisplayName = "Sleep")]
public class SleepParameters : BaseActionParameters
{
    [Required]
    [Display(Name = "Duration (seconds)", Description = "Number of seconds to sleep")]
    [Range(1, 300)]
    [DefaultValue(1)]
    public Resolvable<int> Seconds { get; set; } = 1;

    public override (bool valid, List<ValidationResult> results) IsValid()
    {
        return ValidateDataAnnotations();
    }
}

/// <summary>
/// Output from Sleep action
/// </summary>
public class SleepOutput
{
    /// <summary>
    /// Requested sleep duration in seconds
    /// </summary>
    public int RequestedSeconds { get; set; }

    /// <summary>
    /// Actual duration slept in milliseconds
    /// </summary>
    public long ActualDurationMs { get; set; }
}

/// <summary>
/// Provider for sleep/delay actions
/// </summary>
[ActionProvider]
public class SleepActionProvider
{
    private readonly IParameterResolver _parameterResolver;

    public SleepActionProvider(IParameterResolver parameterResolver)
    {
        _parameterResolver = parameterResolver;
    }

    [ActionMethod("sleep")]
    public async Task<SleepOutput> ExecuteAsync(
        SleepParameters parameters,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var seconds = _parameterResolver.Resolve(parameters.Seconds, context);
        await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

        stopwatch.Stop();

        return new SleepOutput
        {
            RequestedSeconds = seconds,
            ActualDurationMs = stopwatch.ElapsedMilliseconds
        };
    }
}
