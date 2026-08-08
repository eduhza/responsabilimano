using System.Globalization;
using System.Text;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Culture-safe conversion between what a user types and <see cref="decimal"/>
/// (spec X2).
/// <para>
/// The bug this exists to kill: an HTML number input always posts the invariant
/// form ("3.6"), but the app runs under pt-BR, where "." is the *thousands*
/// separator — so <c>decimal.TryParse("3.6")</c> returned 36 and "96.8" returned
/// 968. Parsing here is therefore always explicit about culture and never allows
/// thousands separators to be inferred from a single separator.
/// </para>
/// </summary>
public static class DecimalInput
{
    /// <summary>
    /// Deliberately excludes <see cref="NumberStyles.AllowThousands"/>: after
    /// normalisation the string carries at most one "." and no group separators,
    /// so allowing them could only reintroduce the ambiguity we just removed.
    /// </summary>
    private const NumberStyles Styles =
        NumberStyles.AllowLeadingSign
        | NumberStyles.AllowDecimalPoint
        | NumberStyles.AllowLeadingWhite
        | NumberStyles.AllowTrailingWhite;

    /// <summary>
    /// Parses user input accepting both "," and "." as the decimal separator.
    /// Returns <c>null</c> for empty, malformed or ambiguous input — never throws.
    /// </summary>
    /// <remarks>
    /// Rules, in order:
    /// <list type="number">
    /// <item>both separators present — the one occurring last is the decimal
    /// separator, the other is a group separator (which must group correctly);</item>
    /// <item>one separator appearing once — it is the decimal separator, never a
    /// group separator (nobody types thousands into a weight field, and reading it
    /// as a group separator is exactly the bug we are fixing);</item>
    /// <item>one separator repeated — all occurrences are group separators, and the
    /// digits must group correctly (so "1.234.567" parses and "3,6," does not).</item>
    /// </list>
    /// </remarks>
    public static decimal? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var compact = RemoveWhitespace(text);
        if (compact.Length == 0)
            return null;

        var dots = Count(compact, '.');
        var commas = Count(compact, ',');

        string normalized;

        if (dots > 0 && commas > 0)
        {
            var decimalSeparator = compact.LastIndexOf('.') > compact.LastIndexOf(',') ? '.' : ',';
            var groupSeparator = decimalSeparator == '.' ? ',' : '.';

            var decimalIndex = compact.LastIndexOf(decimalSeparator);
            // A group separator after the decimal separator means the input is
            // malformed ("1,2.3,4"), not a number we should guess at.
            if (compact.LastIndexOf(groupSeparator) > decimalIndex)
                return null;

            if (compact.IndexOf(decimalSeparator) != decimalIndex)
                return null;

            if (!IsValidGrouping(compact[..decimalIndex], groupSeparator))
                return null;

            normalized = string.Concat(
                compact[..decimalIndex].Replace(groupSeparator.ToString(), string.Empty),
                ".",
                compact[(decimalIndex + 1)..]);
        }
        else if (dots + commas == 1)
        {
            normalized = compact.Replace(',', '.');
        }
        else if (dots + commas > 1)
        {
            var groupSeparator = dots > 0 ? '.' : ',';
            if (!IsValidGrouping(compact, groupSeparator))
                return null;

            normalized = compact.Replace(groupSeparator.ToString(), string.Empty);
        }
        else
        {
            normalized = compact;
        }

        return decimal.TryParse(normalized, Styles, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <inheritdoc cref="TryParse(string?)"/>
    public static decimal? TryParse(object? value) => TryParse(value?.ToString());

    /// <summary>
    /// Machine-readable form, always with "." — for CSS lengths, JS interop and any
    /// HTML attribute a browser or spec requires in invariant format.
    /// </summary>
    public static string ToInvariant(decimal? value) =>
        value?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Human-readable form in the current culture (pt-BR renders "3,6"), with the
    /// number of decimals that the goal's data type can actually hold.
    /// </summary>
    public static string ToDisplay(decimal? value, GoalDataType dataType) =>
        value?.ToString(DisplayFormat(dataType), CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>Display form for values not tied to a goal (ranges, averages).</summary>
    public static string ToDisplay(decimal? value) =>
        value?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;

    private static string DisplayFormat(GoalDataType dataType) => dataType switch
    {
        GoalDataType.Integer => "0",
        GoalDataType.Percent => "0.##",
        GoalDataType.Boolean => "0",
        GoalDataType.Scale => "0",
        _ => "0.####"
    };

    private static string RemoveWhitespace(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (!char.IsWhiteSpace(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static int Count(string text, char character)
    {
        var total = 0;
        foreach (var current in text)
        {
            if (current == character)
                total++;
        }

        return total;
    }

    /// <summary>
    /// True when <paramref name="text"/> is an integer whose digits are grouped in
    /// threes by <paramref name="separator"/> — "1.234.567" yes, "3,6," no.
    /// </summary>
    private static bool IsValidGrouping(string text, char separator)
    {
        var digits = text;
        if (digits.StartsWith('-') || digits.StartsWith('+'))
            digits = digits[1..];

        var groups = digits.Split(separator);
        if (groups.Length == 1)
            return groups[0].Length > 0 && groups[0].All(char.IsAsciiDigit);

        if (groups[0].Length is < 1 or > 3 || !groups[0].All(char.IsAsciiDigit))
            return false;

        for (var i = 1; i < groups.Length; i++)
        {
            if (groups[i].Length != 3 || !groups[i].All(char.IsAsciiDigit))
                return false;
        }

        return true;
    }
}
