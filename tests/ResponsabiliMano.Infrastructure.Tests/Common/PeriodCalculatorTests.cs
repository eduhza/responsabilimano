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
}
