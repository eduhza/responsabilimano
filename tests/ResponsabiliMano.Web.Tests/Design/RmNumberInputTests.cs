using System.Globalization;
using Bunit;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Web.Components.Design;

namespace ResponsabiliMano.Web.Tests.Design;

/// <summary>
/// Round-trip bUnit tests for RmNumberInput: user types in pt-BR, the bound
/// decimal is parsed; reopening the field displays the value in pt-BR again
/// (spec X2, AC 4/5).
/// </summary>
public class RmNumberInputTests : TestContext
{
    [Fact]
    public void DisplaysDecimal_InPtBr()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<RmNumberInput>(p =>
                p.Add(x => x.Value, 3.6m)
                 .Add(x => x.DataType, GoalDataType.Decimal));

            Assert.Equal("3,6", cut.Find("input").GetAttribute("value"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void TypeBrazilianDecimal_BindsToInvariantValue()
    {
        var original = CultureInfo.CurrentCulture;
        decimal? boundValue = null;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<RmNumberInput>(p =>
                p.Add(x => x.DataType, GoalDataType.Decimal)
                 .Add(x => x.ValueChanged, (decimal? v) => boundValue = v));

            cut.Find("input").Change("96,8");

            Assert.Equal(96.8m, boundValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void TypeInvariantDecimal_BindsToSameValue()
    {
        var original = CultureInfo.CurrentCulture;
        decimal? boundValue = null;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<RmNumberInput>(p =>
                p.Add(x => x.DataType, GoalDataType.Decimal)
                 .Add(x => x.ValueChanged, (decimal? v) => boundValue = v));

            cut.Find("input").Change("3.6");

            Assert.Equal(3.6m, boundValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ReopenValue_RendersSameBrazilianText()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<RmNumberInput>(p =>
                p.Add(x => x.Value, 3.6m)
                 .Add(x => x.DataType, GoalDataType.Decimal));

            Assert.Equal("3,6", cut.Find("input").GetAttribute("value"));

            // Simulate the user changing nothing and tabbing out.
            cut.Find("input").Change("3,6");

            Assert.Equal("3,6", cut.Find("input").GetAttribute("value"));
            Assert.Equal(3.6m, cut.Instance.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void IntegerDataType_ParsesFractionalButLeavesValidationToCaller()
    {
        var original = CultureInfo.CurrentCulture;
        decimal? boundValue = null;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<RmNumberInput>(p =>
                p.Add(x => x.DataType, GoalDataType.Integer)
                 .Add(x => x.ValueChanged, (decimal? v) => boundValue = v));

            cut.Find("input").Change("3,5");

            // RmNumberInput is a culture-safe parser, not a validator.
            // GoalValueRules rejects fractional integers at the form/service layer.
            Assert.Equal(3.5m, boundValue);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
