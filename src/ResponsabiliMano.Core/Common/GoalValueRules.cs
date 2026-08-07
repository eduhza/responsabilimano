using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Common;

/// <summary>Why a value is not acceptable for a goal (spec X2).</summary>
public enum GoalValueError
{
    NotInteger,
    BelowMinimum,
    AboveMaximum,
    PercentOutOfRange,
    MinGreaterThanMax,
    TargetInconsistentWithDirection
}

/// <summary>
/// The single definition of what a value may be for a given <see cref="GoalDataType"/>
/// (spec X2). Lives in Core as a pure function so the same rule runs on the client
/// (inline message, localised) and on the server (hard guard) without drifting.
/// </summary>
public static class GoalValueRules
{
    /// <summary>Decimals beyond this are noise for the goals this app tracks.</summary>
    public const int DecimalScale = 4;

    private const decimal PercentMinimum = 0m;
    private const decimal PercentMaximum = 100m;

    /// <summary>
    /// Rounds a value to the precision its data type can hold. Integer values are
    /// *not* rounded here — a fractional integer is a validation error, not
    /// something to silently fix (see <see cref="Validate"/>).
    /// </summary>
    public static decimal Normalize(GoalDataType dataType, decimal value) => dataType switch
    {
        GoalDataType.Percent => Math.Round(value, 2, MidpointRounding.ToEven),
        GoalDataType.Integer => value,
        _ => Math.Round(value, DecimalScale, MidpointRounding.ToEven)
    };

    /// <summary>
    /// Validates a reported value against its data type and the goal's bounds.
    /// Returns <c>null</c> when acceptable.
    /// </summary>
    public static GoalValueError? Validate(
        GoalDataType dataType,
        decimal value,
        decimal? minValue,
        decimal? maxValue)
    {
        if (dataType == GoalDataType.Integer && decimal.Truncate(value) != value)
            return GoalValueError.NotInteger;

        if (dataType == GoalDataType.Percent && (value < PercentMinimum || value > PercentMaximum))
            return GoalValueError.PercentOutOfRange;

        if (minValue is { } min && value < min)
            return GoalValueError.BelowMinimum;

        if (maxValue is { } max && value > max)
            return GoalValueError.AboveMaximum;

        return null;
    }

    /// <summary>
    /// Validates a goal *definition* — its bounds must obey the same type rules the
    /// reported values will, otherwise the goal is unfillable.
    /// </summary>
    public static GoalValueError? ValidateDefinition(
        GoalDataType dataType,
        decimal? minValue,
        decimal? maxValue)
    {
        if (minValue is { } min && maxValue is { } max && min > max)
            return GoalValueError.MinGreaterThanMax;

        foreach (var bound in new[] { minValue, maxValue })
        {
            // Bounds are checked against the type only: comparing a bound to itself
            // would be circular.
            if (bound is { } value && Validate(dataType, value, null, null) is { } error)
                return error;
        }

        return null;
    }

    /// <summary>
    /// Validates a per-participant target (baseline and target value) for a goal.
    /// Returns <c>null</c> when acceptable.
    /// </summary>
    public static GoalValueError? ValidateTarget(
        GoalDataType dataType,
        decimal? minValue,
        decimal? maxValue,
        decimal? baseline,
        decimal? targetValue,
        GoalDirection direction)
    {
        if (targetValue is { } target && Validate(dataType, target, minValue, maxValue) is { } targetError)
            return targetError;

        if (baseline is { } b && Validate(dataType, b, minValue, maxValue) is { } baselineError)
            return baselineError;

        if (baseline is not null && targetValue is not null)
        {
            if (direction == GoalDirection.Decrease && targetValue >= baseline)
                return GoalValueError.TargetInconsistentWithDirection;

            if (direction == GoalDirection.Increase && targetValue <= baseline)
                return GoalValueError.TargetInconsistentWithDirection;
        }

        return null;
    }

    /// <summary>
    /// English fallback text for logs and API responses. User-facing copy is
    /// localised in the Web layer from <see cref="GoalValueError"/>.
    /// </summary>
    public static string Describe(GoalValueError error, string goalLabel, decimal? minValue, decimal? maxValue, decimal? targetValue = null) => error switch
    {
        GoalValueError.NotInteger => $"Value for '{goalLabel}' must be a whole number.",
        GoalValueError.PercentOutOfRange => $"Value for '{goalLabel}' must be between 0 and 100.",
        GoalValueError.BelowMinimum => $"Value for '{goalLabel}' is below the minimum of {minValue}.",
        GoalValueError.AboveMaximum => $"Value for '{goalLabel}' is above the maximum of {maxValue}.",
        GoalValueError.MinGreaterThanMax => $"Minimum for '{goalLabel}' cannot be greater than its maximum.",
        GoalValueError.TargetInconsistentWithDirection => $"Target for '{goalLabel}' is not consistent with its direction.",
        _ => $"Value for '{goalLabel}' is invalid."
    };
}

/// <summary>
/// Thrown when a goal value or definition breaks <see cref="GoalValueRules"/>.
/// Derives from <see cref="ArgumentException"/> so existing 400-mapping and UI
/// catch blocks keep working, while callers that care can localise
/// <see cref="Error"/> instead of showing the English message.
/// </summary>
public sealed class GoalValueException : ArgumentException
{
    public GoalValueException(
        GoalValueError error,
        GoalDataType dataType,
        string goalLabel,
        decimal? minValue,
        decimal? maxValue,
        decimal? targetValue = null,
        decimal? baseline = null)
        : base(GoalValueRules.Describe(error, goalLabel, minValue, maxValue, targetValue))
    {
        Error = error;
        DataType = dataType;
        GoalLabel = goalLabel;
        MinValue = minValue;
        MaxValue = maxValue;
        TargetValue = targetValue;
        Baseline = baseline;
    }

    public GoalValueError Error { get; }
    public GoalDataType DataType { get; }
    public string GoalLabel { get; }
    public decimal? MinValue { get; }
    public decimal? MaxValue { get; }
    public decimal? TargetValue { get; }
    public decimal? Baseline { get; }
}
