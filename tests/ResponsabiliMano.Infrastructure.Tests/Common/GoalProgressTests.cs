using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

public class GoalProgressTests
{
    public static TheoryData<GoalDirection, decimal?, decimal?, decimal, decimal?, decimal?> ProgressCases => new()
    {
        // Decrease: baseline 100, target 90, current 95 -> 50%
        { GoalDirection.Decrease, 100m, null, 95m, 90m, 50m },
        // Decrease: baseline 100, target 90, current 90 -> 100%
        { GoalDirection.Decrease, 100m, null, 90m, 90m, 100m },
        // Decrease: baseline 100, target 90, current 100 -> 0%
        { GoalDirection.Decrease, 100m, null, 100m, 90m, 0m },
        // Decrease: baseline 100, target 90, current 85 -> clamped 100%
        { GoalDirection.Decrease, 100m, null, 85m, 90m, 100m },
        // Increase: baseline 10, target 20, current 15 -> 50%
        { GoalDirection.Increase, 10m, null, 15m, 20m, 50m },
        // Increase: baseline 10, target 20, current 25 -> clamped 100%
        { GoalDirection.Increase, 10m, null, 25m, 20m, 100m },
        // Increase: baseline 10, target 20, current 5 -> 0%
        { GoalDirection.Increase, 10m, null, 5m, 20m, 0m },
        // Reach: target 90, current 45 -> 50%
        { GoalDirection.Reach, null, null, 45m, 90m, 50m },
        // Reach: target 90, current 90 -> 100%
        { GoalDirection.Reach, null, null, 90m, 90m, 100m },
        // Reach: target 0 -> null
        { GoalDirection.Reach, null, null, 50m, 0m, null },
        // Maintain: target 50, current 25 -> 50%
        { GoalDirection.Maintain, null, null, 25m, 50m, 50m },
        // Maintain: target 50, current 50 -> 100%
        { GoalDirection.Maintain, null, null, 50m, 50m, 100m },
        // Maintain: target 0 -> null
        { GoalDirection.Maintain, null, null, 50m, 0m, null }
    };

    [Theory]
    [MemberData(nameof(ProgressCases))]
    public void Percent_ComputesProgress(
        GoalDirection direction,
        decimal? baseline,
        decimal? firstCheckIn,
        decimal current,
        decimal? target,
        decimal? expected)
    {
        Assert.Equal(expected, GoalProgress.Percent(baseline, current, target, direction, firstCheckIn));
    }

    public static TheoryData<decimal?, decimal?, decimal, decimal, decimal?> FirstCheckInCases => new()
    {
        { null, 100m, 95m, 90m, 50m }, // baseline null, first check-in 100, current 95 -> 50%
        { null, 100m, 105m, 90m, 0m }   // baseline null, current above baseline -> 0%
    };

    [Theory]
    [MemberData(nameof(FirstCheckInCases))]
    public void Percent_UsesFirstCheckIn_WhenBaselineIsNull(
        decimal? baseline, decimal? firstCheckIn, decimal current, decimal target, decimal? expected)
    {
        Assert.Equal(expected, GoalProgress.Percent(baseline, current, target, GoalDirection.Decrease, firstCheckIn));
    }

    public static TheoryData<decimal?, decimal?, decimal, decimal> NullCases => new()
    {
        { 100m, null, 90m, 100m }, // baseline == target for decrease -> null
        { null, null, 90m, 0m }     // reach with target 0 -> null
    };

    [Theory]
    [MemberData(nameof(NullCases))]
    public void Percent_ReturnsNull_WhenDirectionCannotBeComputed(
        decimal? baseline, decimal? firstCheckIn, decimal current, decimal target)
    {
        Assert.Null(GoalProgress.Percent(baseline, current, target, GoalDirection.Decrease, firstCheckIn));
    }

    [Fact]
    public void Percent_NullTarget_ReturnsNull()
    {
        Assert.Null(GoalProgress.Percent(100m, 95m, null, GoalDirection.Decrease));
    }

    [Fact]
    public void Percent_NegativeOrOverHundred_IsClamped()
    {
        Assert.Equal(0m, GoalProgress.Percent(100m, 110m, 90m, GoalDirection.Decrease));
        Assert.Equal(100m, GoalProgress.Percent(100m, 80m, 90m, GoalDirection.Decrease));
    }
}
