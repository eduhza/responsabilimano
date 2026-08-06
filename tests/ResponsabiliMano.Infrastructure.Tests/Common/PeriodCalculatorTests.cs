using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

public class PeriodCalculatorTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CurrentPeriod_ReturnsZero_BeforeStart()
    {
        var now = Start.AddDays(-1);
        Assert.Equal(0, PeriodCalculator.CurrentPeriod(Start, ProjectFrequency.Weekly, now));
    }

    [Fact]
    public void CurrentPeriod_ReturnsOne_OnStartDay_IgnoringTimeOfDay()
    {
        // Earlier clock time on the start day still counts as period 1.
        var now = Start.AddHours(-3);
        Assert.Equal(1, PeriodCalculator.CurrentPeriod(Start, ProjectFrequency.Weekly, now));
    }

    [Theory]
    [InlineData(ProjectFrequency.Daily, 0, 1)]
    [InlineData(ProjectFrequency.Daily, 1, 2)]
    [InlineData(ProjectFrequency.Daily, 9, 10)]
    [InlineData(ProjectFrequency.Weekly, 6, 1)]
    [InlineData(ProjectFrequency.Weekly, 7, 2)]
    [InlineData(ProjectFrequency.Weekly, 14, 3)]
    [InlineData(ProjectFrequency.Biweekly, 13, 1)]
    [InlineData(ProjectFrequency.Biweekly, 14, 2)]
    [InlineData(ProjectFrequency.Biweekly, 28, 3)]
    public void CurrentPeriod_CountsWholePeriods(ProjectFrequency frequency, int elapsedDays, int expected)
    {
        var now = Start.AddDays(elapsedDays);
        Assert.Equal(expected, PeriodCalculator.CurrentPeriod(Start, frequency, now));
    }

    [Theory]
    [InlineData(0, 1)]    // same day
    [InlineData(20, 1)]   // 20 days in, not yet a full month
    [InlineData(31, 2)]   // Feb 1
    [InlineData(59, 3)]   // Mar 1 (2026 is not a leap year: 31 + 28)
    public void CurrentPeriod_Monthly_CountsCalendarMonths(int elapsedDays, int expected)
    {
        var now = Start.AddDays(elapsedDays);
        Assert.Equal(expected, PeriodCalculator.CurrentPeriod(Start, ProjectFrequency.Monthly, now));
    }

    [Theory]
    [InlineData(ProjectFrequency.Daily, 1, 0, 1)]
    [InlineData(ProjectFrequency.Daily, 2, 1, 2)]
    [InlineData(ProjectFrequency.Weekly, 1, 0, 1)]
    [InlineData(ProjectFrequency.Weekly, 2, 7, 2)]
    [InlineData(ProjectFrequency.Biweekly, 2, 14, 2)]
    public void PeriodStart_IgnoresTimeOfDay_ForNonMonthly(ProjectFrequency frequency, int period, int expectedOffsetDays, int expectedPeriod)
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var startOfPeriod = PeriodCalculator.PeriodStart(start, frequency, period);
        Assert.Equal(Start.AddDays(expectedOffsetDays).Date, startOfPeriod.Date);
        Assert.Equal(DateTimeKind.Utc, startOfPeriod.Kind);
        Assert.Equal(expectedPeriod, PeriodCalculator.CurrentPeriod(start, frequency, startOfPeriod));
    }

    [Fact]
    public void PeriodStart_Monthly_HandlesShortMonths()
    {
        var start = new DateTime(2026, 1, 31, 8, 0, 0, DateTimeKind.Utc);

        // Period 1 starts Jan 31.
        Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), PeriodCalculator.PeriodStart(start, ProjectFrequency.Monthly, 1));

        // February has no 31st, so period 2 starts on the 1st of March.
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), PeriodCalculator.PeriodStart(start, ProjectFrequency.Monthly, 2));

        // March has a 31st.
        Assert.Equal(new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc), PeriodCalculator.PeriodStart(start, ProjectFrequency.Monthly, 3));
    }

    [Fact]
    public void PeriodEnd_ReturnsEndOfDayBeforeNextPeriod()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var nextStart = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc);

        var end = PeriodCalculator.PeriodEnd(start, ProjectFrequency.Weekly, 1);

        Assert.Equal(nextStart.AddTicks(-1), end);
        Assert.Equal(DateTimeKind.Utc, end.Kind);
    }

    [Fact]
    public void PeriodEnd_ClampsToProjectEnd()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var projectEnd = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var clampEnd = projectEnd.Date.AddDays(1).AddTicks(-1);

        var end = PeriodCalculator.PeriodEnd(start, ProjectFrequency.Weekly, 2, projectEnd);

        Assert.Equal(clampEnd, end);
        Assert.Equal(DateTimeKind.Utc, end.Kind);
    }
}
