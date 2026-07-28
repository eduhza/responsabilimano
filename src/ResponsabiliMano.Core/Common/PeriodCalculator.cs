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
}
