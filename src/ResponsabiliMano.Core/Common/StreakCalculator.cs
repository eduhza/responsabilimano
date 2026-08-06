namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Turns the set of periods a participant checked in on into a streak pair. The
/// current streak counts back from the most recent check-in while the periods stay
/// consecutive; the best streak is the longest consecutive run anywhere in the
/// history. The rule lives here so the project screen and the global panel cannot
/// drift apart.
/// </summary>
public static class StreakCalculator
{
    /// <summary>
    /// Order and duplicates in <paramref name="periodNumbers"/> do not matter: the
    /// input is normalised before counting. Returns <c>(0, 0)</c> when empty.
    /// </summary>
    public static (int Current, int Best) FromPeriods(IEnumerable<int> periodNumbers)
    {
        var periods = periodNumbers
            .Distinct()
            .OrderByDescending(p => p)
            .ToList();

        if (periods.Count == 0)
            return (0, 0);

        var current = 0;
        int? expected = null;
        foreach (var period in periods)
        {
            if (expected is null)
            {
                current = 1;
                expected = period - 1;
            }
            else if (period == expected)
            {
                current++;
                expected = period - 1;
            }
            else
            {
                break;
            }
        }

        var best = 0;
        var run = 0;
        int? runExpected = null;
        foreach (var period in periods)
        {
            if (runExpected is null || period == runExpected)
            {
                run++;
                runExpected = period - 1;
            }
            else
            {
                if (run > best)
                    best = run;

                run = 1;
                runExpected = period - 1;
            }
        }

        if (run > best)
            best = run;

        return (current, best);
    }
}
