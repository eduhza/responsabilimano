using ResponsabiliMano.Core.Common;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

/// <summary>
/// Unit tests for the streak rule extracted in spec S6.1, so the project screen and
/// the account-wide panel share one definition.
/// </summary>
public class StreakCalculatorTests
{
    [Fact]
    public void Empty_history_has_no_streak()
    {
        Assert.Equal((0, 0), StreakCalculator.FromPeriods([]));
    }

    [Fact]
    public void Single_check_in_counts_as_one()
    {
        Assert.Equal((1, 1), StreakCalculator.FromPeriods([4]));
    }

    [Fact]
    public void Consecutive_periods_count_from_the_most_recent()
    {
        Assert.Equal((3, 3), StreakCalculator.FromPeriods([1, 2, 3]));
    }

    [Fact]
    public void Current_streak_stops_at_the_first_gap_while_best_keeps_the_longest_run()
    {
        // Periods 1-2-3 then a gap, then 6-7: the pair is still on a 2-run, but the
        // record is the earlier 3-run.
        var (current, best) = StreakCalculator.FromPeriods([1, 2, 3, 6, 7]);

        Assert.Equal(2, current);
        Assert.Equal(3, best);
    }

    [Fact]
    public void Input_order_and_duplicates_do_not_change_the_result()
    {
        Assert.Equal(
            StreakCalculator.FromPeriods([1, 2, 3]),
            StreakCalculator.FromPeriods([3, 1, 2, 3]));
    }

    [Fact]
    public void Isolated_check_ins_never_build_a_streak()
    {
        var (current, best) = StreakCalculator.FromPeriods([1, 3, 5]);

        Assert.Equal(1, current);
        Assert.Equal(1, best);
    }
}
