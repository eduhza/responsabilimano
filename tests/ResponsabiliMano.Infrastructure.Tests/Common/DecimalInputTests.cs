using System.Globalization;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

/// <summary>Pure parsing/formatting tests for the culture-safe decimal helper (spec X2).</summary>
public class DecimalInputTests
{
    public static TheoryData<string?, decimal?> TryParseCases => new()
    {
        { "3,6", 3.6m },
        { "3.6", 3.6m },
        { "96,8", 96.8m },
        { "96.8", 96.8m },
        { "1.234,56", 1234.56m },
        { "1,234.56", 1234.56m },
        { "1.234.567", 1234567m },
        { "1,234,567", 1234567m },
        { "1234", 1234m },
        { "-2,5", -2.5m },
        { " 3,6 ", 3.6m },
        { string.Empty, null },
        { null, null },
        { "abc", null },
        { "3,6,", null },
        { "1.23.4", null },
        { "1,2.3,4", null }
    };

    [Theory]
    [MemberData(nameof(TryParseCases))]
    public void TryParse_ReturnsExpected(string? input, decimal? expected)
    {
        Assert.Equal(expected, DecimalInput.TryParse(input));
    }

    [Fact]
    public void ToInvariant_FormatsWithDot()
    {
        Assert.Equal("3.6", DecimalInput.ToInvariant(3.6m));
        Assert.Equal("-2.5", DecimalInput.ToInvariant(-2.5m));
        Assert.Equal(string.Empty, DecimalInput.ToInvariant(null));
    }

    [Fact]
    public void ToDisplay_UsesCurrentCultureAndDataType()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            Assert.Equal("3,6", DecimalInput.ToDisplay(3.6m, GoalDataType.Decimal));
            Assert.Equal("96,8", DecimalInput.ToDisplay(96.8m, GoalDataType.Decimal));
            Assert.Equal("10", DecimalInput.ToDisplay(10m, GoalDataType.Integer));
            Assert.Equal("50", DecimalInput.ToDisplay(50m, GoalDataType.Percent));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
