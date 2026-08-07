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
        { GoalDataType.Integer, 5m, 0m, 10m, null }
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
        { GoalDataType.Decimal, 25m, 10m, 20m, GoalValueError.AboveMaximum }
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
        { GoalDataType.Integer, 7m, 7m }
    };

    [Theory]
    [MemberData(nameof(NormalizeCases))]
    public void Normalize_RoundsToDataTypePrecision(GoalDataType dataType, decimal value, decimal expected)
    {
        Assert.Equal(expected, GoalValueRules.Normalize(dataType, value));
    }

    public static TheoryData<GoalDataType, decimal?, decimal?, decimal?, GoalValueError?> DefinitionCases => new()
    {
        { GoalDataType.Decimal, 0m, 10m, 5m, null },
        { GoalDataType.Integer, 0m, 10m, 5.5m, GoalValueError.NotInteger },
        { GoalDataType.Percent, 0m, 10m, 5m, null },
        { GoalDataType.Decimal, 20m, 10m, 5m, GoalValueError.MinGreaterThanMax }
    };

    [Theory]
    [MemberData(nameof(DefinitionCases))]
    public void ValidateDefinition_EnforcesTypeAndBounds(
        GoalDataType dataType, decimal? min, decimal? max, decimal? target, GoalValueError? expected)
    {
        Assert.Equal(expected, GoalValueRules.ValidateDefinition(dataType, min, max, target));
    }
}
