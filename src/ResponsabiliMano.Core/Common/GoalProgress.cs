using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Pure function for 0..100 progress relative to a per-participant goal target (spec S7.2).
/// Never throws: invalid inputs return <c>null</c> so the UI simply omits the bar.
/// </summary>
public static class GoalProgress
{
    /// <summary>
    /// Computes progress for the given direction.
    /// </summary>
    /// <param name="baseline">Optional baseline. When null and a first check-in is supplied, that value is used. When still null, Decrease/Increase fall back to Reach.</param>
    /// <param name="current">The current value (e.g. average or latest check-in).</param>
    /// <param name="target">The target value. Null or a divisor of zero yields null.</param>
    /// <param name="direction">How the metric is intended to evolve.</param>
    /// <param name="firstCheckIn">The value of the user's first check-in for this goal, used when <paramref name="baseline"/> is null.</param>
    /// <returns>A value in [0, 100] or <c>null</c> when progress cannot be computed.</returns>
    public static decimal? Percent(
        decimal? baseline,
        decimal current,
        decimal? target,
        GoalDirection direction,
        decimal? firstCheckIn = null)
    {
        if (target is not { } t)
            return null;

        var effectiveBaseline = baseline ?? firstCheckIn;

        if (direction == GoalDirection.Reach)
            return ComputeReach(current, t);

        if (direction == GoalDirection.Maintain)
            return ComputeMaintain(current, t);

        if (effectiveBaseline is not { } b)
            return ComputeReach(current, t);

        if (direction == GoalDirection.Decrease)
            return ComputeDecrease(current, b, t);

        if (direction == GoalDirection.Increase)
            return ComputeIncrease(current, b, t);

        return null;
    }

    private static decimal? ComputeDecrease(decimal current, decimal baseline, decimal target)
    {
        if (baseline == target)
            return null;

        var denominator = baseline - target;
        if (denominator == 0m)
            return null;

        var value = (baseline - current) / denominator;
        return ClampPercent(value);
    }

    private static decimal? ComputeIncrease(decimal current, decimal baseline, decimal target)
    {
        if (baseline == target)
            return null;

        var denominator = target - baseline;
        if (denominator == 0m)
            return null;

        var value = (current - baseline) / denominator;
        return ClampPercent(value);
    }

    private static decimal? ComputeReach(decimal current, decimal target)
    {
        if (target == 0m)
            return null;

        return ClampPercent(current / target);
    }

    private static decimal? ComputeMaintain(decimal current, decimal target)
    {
        if (target == 0m)
            return null;

        var value = 1m - Math.Abs(current - target) / Math.Abs(target);
        return ClampPercent(value);
    }

    private static decimal ClampPercent(decimal value) =>
        Math.Clamp(Math.Round(value * 100m, 2), 0m, 100m);
}
