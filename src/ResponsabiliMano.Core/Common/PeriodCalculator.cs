using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Maps a project's <see cref="ProjectFrequency"/> and start date to the 1-based
/// period number that is current at a given instant. Period 1 begins on the start
/// date; the calculation is date-based (time of day is ignored) so a check-in
/// submitted any time on the same calendar day falls in the same period.
/// </summary>
public static class PeriodCalculator
{
    /// <summary>
    /// Returns the 1-based current period, or 0 when <paramref name="nowUtc"/>
    /// precedes <paramref name="startDateUtc"/> (the project has not started).
    /// </summary>
    public static int CurrentPeriod(DateTime startDateUtc, ProjectFrequency frequency, DateTime nowUtc)
    {
        if (nowUtc.Date < startDateUtc.Date)
            return 0;

        var elapsedDays = (int)(nowUtc.Date - startDateUtc.Date).TotalDays;

        return frequency switch
        {
            ProjectFrequency.Daily => elapsedDays + 1,
            ProjectFrequency.Weekly => elapsedDays / 7 + 1,
            ProjectFrequency.Biweekly => elapsedDays / 14 + 1,
            ProjectFrequency.Monthly => MonthsElapsed(startDateUtc, nowUtc) + 1,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported frequency.")
        };
    }

    private static int MonthsElapsed(DateTime start, DateTime now)
    {
        var months = (now.Year - start.Year) * 12 + (now.Month - start.Month);
        if (now.Day < start.Day)
            months--; // the current month has not completed a full period yet

        return Math.Max(0, months);
    }

    /// <summary>
    /// Returns the instant the 1-based <paramref name="periodNumber"/> begins,
    /// ignoring the time-of-day of <paramref name="startDateUtc"/> so the period
    /// covers whole calendar days.
    /// </summary>
    public static DateTime PeriodStart(DateTime startDateUtc, ProjectFrequency frequency, int periodNumber)
    {
        if (periodNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(periodNumber), periodNumber, "Period number must be 1 or greater.");

        var start = frequency switch
        {
            ProjectFrequency.Daily => startDateUtc.Date.AddDays(periodNumber - 1),
            ProjectFrequency.Weekly => startDateUtc.Date.AddDays((periodNumber - 1) * 7),
            ProjectFrequency.Biweekly => startDateUtc.Date.AddDays((periodNumber - 1) * 14),
            ProjectFrequency.Monthly => MonthlyPeriodStart(startDateUtc, periodNumber),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported frequency.")
        };

        return DateTime.SpecifyKind(start, DateTimeKind.Utc);
    }

    private static DateTime MonthlyPeriodStart(DateTime startDate, int periodNumber)
    {
        // Period N is the earliest date in month offset N-1 that is in the Nth period.
        // If month N-1 has at least start.Day days, that's the start day of that month.
        // Otherwise the period starts on the 1st of the following month.
        var candidate = startDate.Date.AddMonths(periodNumber - 1);

        if (candidate.Day == startDate.Day)
            return candidate;

        var firstDayOfFollowingMonth = new DateTime(candidate.Year, candidate.Month, 1)
            .AddMonths(1);
        return firstDayOfFollowingMonth;
    }

    /// <summary>
    /// Returns the inclusive end of the 1-based <paramref name="periodNumber"/>.
    /// The next period begins one tick after this value. If <paramref name="clampToUtc"/>
    /// is provided and the natural end falls beyond it, the result is clamped to the
    /// end of the clamp day (23:59:59.9999999 UTC).
    /// </summary>
    public static DateTime PeriodEnd(DateTime startDateUtc, ProjectFrequency frequency, int periodNumber, DateTime? clampToUtc = null)
    {
        var nextStart = PeriodStart(startDateUtc, frequency, periodNumber + 1);
        var end = nextStart.AddTicks(-1);

        if (clampToUtc is { } clamp)
        {
            var clampEnd = clamp.Date.AddDays(1).AddTicks(-1);
            if (end > clampEnd)
                end = clampEnd;
        }

        return DateTime.SpecifyKind(end, DateTimeKind.Utc);
    }
}
