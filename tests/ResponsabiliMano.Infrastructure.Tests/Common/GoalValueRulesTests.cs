using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

/// <summary>Goal value and definition validation rules (spec X2).</summary>
public class GoalValueRulesTests
{
    public static TheoryData<GoalDataType, decimal, decimal?, decimal?, GoalValueError?> ValidCases => new()
    {
        { GoalDataType.Integer, 5m, null, null, null },
        { GoalDataType.Percent, 50m, 0m, 100m, null },
        { GoalDataType.Decimal, 3.6m, 0m, 100m, null },
        { GoalDataType.Integer, 5m, 0m, 10m, null },
        { GoalDataType.Boolean, 0m, 0m, 1m, null },
        { GoalDataType.Boolean, 1m, 0m, 1m, null },
        { GoalDataType.Scale, 3m, 1m, 5m, null }
    };

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Validate_ReturnsNull_WhenAcceptable(
        GoalDataType dataType, decimal value, decimal? min, decimal? max, GoalValueError? expected)
    {
        Assert.Equal(expected, GoalValueRules.Validate(dataType, value, min, max));
    }

    public static TheoryData<GoalDataType, decimal, decimal?, decimal?, GoalValueError> InvalidCases => new()
    {
        { GoalDataType.Integer, 5.5m, null, null, GoalValueError.NotInteger },
        { GoalDataType.Percent, 150m, null, null, GoalValueError.PercentOutOfRange },
        { GoalDataType.Percent, -5m, null, null, GoalValueError.PercentOutOfRange },
        { GoalDataType.Decimal, 5m, 10m, 20m, GoalValueError.BelowMinimum },
        { GoalDataType.Decimal, 25m, 10m, 20m, GoalValueError.AboveMaximum },
        { GoalDataType.Boolean, 0.5m, 0m, 1m, GoalValueError.BooleanOutOfRange },
        { GoalDataType.Boolean, 2m, 0m, 1m, GoalValueError.BooleanOutOfRange },
        { GoalDataType.Boolean, -1m, 0m, 1m, GoalValueError.BooleanOutOfRange },
        { GoalDataType.Scale, 3.5m, 1m, 5m, GoalValueError.NotInteger },
        { GoalDataType.Scale, 6m, 1m, 5m, GoalValueError.AboveMaximum },
        { GoalDataType.Scale, 0m, 1m, 5m, GoalValueError.BelowMinimum }
    };

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Validate_ReturnsError_WhenInvalid(
        GoalDataType dataType, decimal value, decimal? min, decimal? max, GoalValueError expected)
    {
        Assert.Equal(expected, GoalValueRules.Validate(dataType, value, min, max));
    }

    public static TheoryData<GoalDataType, decimal, decimal> NormalizeCases => new()
    {
        { GoalDataType.Decimal, 1.23456m, 1.2346m },
        { GoalDataType.Decimal, 1.2344m, 1.2344m },
        { GoalDataType.Percent, 33.333m, 33.33m },
        { GoalDataType.Integer, 7m, 7m },
        { GoalDataType.Boolean, 1m, 1m },
        { GoalDataType.Boolean, 0m, 0m },
        { GoalDataType.Boolean, 5m, 1m },
        { GoalDataType.Scale, 3.9m, 3m },
        { GoalDataType.Scale, 4m, 4m }
    };

    [Theory]
    [MemberData(nameof(NormalizeCases))]
    public void Normalize_RoundsToDataTypePrecision(GoalDataType dataType, decimal value, decimal expected)
    {
        Assert.Equal(expected, GoalValueRules.Normalize(dataType, value));
    }

    public static TheoryData<GoalDataType, decimal?, decimal?, GoalValueError?> DefinitionCases => new()
    {
        { GoalDataType.Decimal, 0m, 10m, null },
        { GoalDataType.Integer, 0m, 10m, null },
        { GoalDataType.Percent, 0m, 10m, null },
        { GoalDataType.Decimal, 20m, 10m, GoalValueError.MinGreaterThanMax },
        { GoalDataType.Boolean, 0m, 1m, null },
        { GoalDataType.Boolean, 1m, 1m, GoalValueError.BooleanOutOfRange },
        { GoalDataType.Scale, 1m, 5m, null },
        { GoalDataType.Scale, 1.5m, 5m, GoalValueError.NotInteger },
        { GoalDataType.Scale, 1m, 1m, GoalValueError.ScaleBoundsInvalid },
        { GoalDataType.Scale, null, 5m, GoalValueError.ScaleBoundsInvalid }
    };

    [Theory]
    [MemberData(nameof(DefinitionCases))]
    public void ValidateDefinition_EnforcesTypeAndBounds(
        GoalDataType dataType, decimal? min, decimal? max, GoalValueError? expected)
    {
        Assert.Equal(expected, GoalValueRules.ValidateDefinition(dataType, min, max));
    }

    public static TheoryData<GoalDataType, decimal?, decimal?, decimal?, decimal?, GoalDirection, GoalValueError?> TargetCases => new()
    {
        { GoalDataType.Decimal, 0m, 200m, 96.8m, 86.8m, GoalDirection.Decrease, null },
        { GoalDataType.Decimal, 0m, 200m, 5m, 6m, GoalDirection.Increase, null },
        { GoalDataType.Decimal, 0m, 200m, 96.8m, 86.8m, GoalDirection.Increase, GoalValueError.TargetInconsistentWithDirection },
        { GoalDataType.Decimal, 0m, 200m, 86.8m, 96.8m, GoalDirection.Decrease, GoalValueError.TargetInconsistentWithDirection },
        { GoalDataType.Percent, 0m, 100m, null, 90m, GoalDirection.Reach, null },
        { GoalDataType.Integer, 0m, 10m, 0m, 5m, GoalDirection.Maintain, null },
        { GoalDataType.Integer, 0m, 10m, 5.5m, 5m, GoalDirection.Reach, GoalValueError.NotInteger },
        { GoalDataType.Boolean, 0m, 1m, 0m, 1m, GoalDirection.Reach, null },
        { GoalDataType.Boolean, 0m, 1m, 0m, 0m, GoalDirection.Reach, GoalValueError.BooleanTargetValueInvalid },
        { GoalDataType.Boolean, 0m, 1m, 0m, 1m, GoalDirection.Increase, GoalValueError.BooleanInvalidDirection },
        { GoalDataType.Scale, 1m, 5m, 1m, 5m, GoalDirection.Reach, null },
        { GoalDataType.Scale, 1m, 5m, 1m, 5m, GoalDirection.Increase, null },
        { GoalDataType.Scale, 1m, 5m, 1m, 5m, GoalDirection.Maintain, GoalValueError.ScaleInvalidDirection },
        { GoalDataType.Scale, 1m, 5m, 2m, 1m, GoalDirection.Increase, GoalValueError.TargetInconsistentWithDirection }
    };

    [Theory]
    [MemberData(nameof(TargetCases))]
    public void ValidateTarget_EnforcesTypeBoundsAndDirection(
        GoalDataType dataType, decimal? min, decimal? max, decimal? baseline, decimal? target, GoalDirection direction, GoalValueError? expected)
    {
        Assert.Equal(expected, GoalValueRules.ValidateTarget(dataType, min, max, baseline, target, direction));
    }
}
