using System.Text.RegularExpressions;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Quartz;

public static class CronHelper
{
    private static readonly Regex FiveFieldCronRegex = new(
        @"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)$",
        RegexOptions.Compiled);

    private static readonly Regex SevenFieldCronRegex = new(
        @"^(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)(\s+\S+)?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Normalizes a cron expression to Quartz 7-field format.
    /// Detects 5-field Linux cron and auto-translates.
    /// </summary>
    public static string NormalizeToQuartzCron(string input)
    {
        var trimmed = input.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is 6 or 7)
            return trimmed;

        if (parts.Length != 5)
            throw new ArgumentException($"Invalid cron expression: expected 5, 6, or 7 fields, got {parts.Length}.");

        var minute = parts[0];
        var hour = parts[1];
        var dayOfMonth = parts[2];
        var month = parts[3];
        var dayOfWeek = TranslateDayOfWeek(parts[4]);

        if (dayOfMonth != "*" && dayOfMonth != "?" && dayOfWeek != "*" && dayOfWeek != "?")
            dayOfWeek = "?";
        else if (dayOfMonth == "*" && dayOfWeek != "*" && dayOfWeek != "?")
            dayOfMonth = "?";
        else if (dayOfWeek == "*")
            dayOfWeek = "?";

        return $"0 {minute} {hour} {dayOfMonth} {month} {dayOfWeek}";
    }

    /// <summary>
    /// Validates a Quartz cron expression is syntactically correct.
    /// </summary>
    public static bool IsValid(string cronExpression)
    {
        return CronExpression.IsValidExpression(cronExpression);
    }

    /// <summary>
    /// Validates that a cron expression doesn't fire more frequently than the specified minimum interval.
    /// </summary>
    public static bool MeetsMinimumInterval(string cronExpression, int minimumIntervalHours)
    {
        try
        {
            var cron = new CronExpression(cronExpression);
            var now = DateTimeOffset.UtcNow;
            var fireTimes = new List<DateTimeOffset>();

            var next = cron.GetNextValidTimeAfter(now);
            for (var i = 0; i < 5 && next.HasValue; i++)
            {
                fireTimes.Add(next.Value);
                next = cron.GetNextValidTimeAfter(next.Value);
            }

            if (fireTimes.Count < 2)
                return true;

            var minimumInterval = TimeSpan.FromHours(minimumIntervalHours);
            for (var i = 1; i < fireTimes.Count; i++)
            {
                if (fireTimes[i] - fireTimes[i - 1] < minimumInterval)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Translates Linux-style day-of-week (0=Sunday) to Quartz-style (1=Sunday).
    /// </summary>
    private static string TranslateDayOfWeek(string dow)
    {
        if (int.TryParse(dow, out var numeric))
            return (numeric + 1).ToString();

        return dow.ToUpperInvariant() switch
        {
            "SUN" => "1",
            "MON" => "2",
            "TUE" => "3",
            "WED" => "4",
            "THU" => "5",
            "FRI" => "6",
            "SAT" => "7",
            _ => dow
        };
    }
}
