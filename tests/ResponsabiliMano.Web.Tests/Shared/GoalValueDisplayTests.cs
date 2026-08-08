using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Web.Components.Shared;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Shared;

/// <summary>bUnit tests for GoalValueDisplay (spec S7.4).</summary>
public class GoalValueDisplayTests : TestContext
{
    public GoalValueDisplayTests()
    {
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
    }

    [Fact]
    public void Boolean_One_RendersYes()
    {
        var cut = RenderComponent<GoalValueDisplay>(p =>
            p.Add(x => x.Value, 1m)
             .Add(x => x.DataType, GoalDataType.Boolean));

        Assert.Contains("BooleanYes", cut.Markup);
    }

    [Fact]
    public void Boolean_Zero_RendersNo()
    {
        var cut = RenderComponent<GoalValueDisplay>(p =>
            p.Add(x => x.Value, 0m)
             .Add(x => x.DataType, GoalDataType.Boolean));

        Assert.Contains("BooleanNo", cut.Markup);
    }

    [Fact]
    public void Boolean_Rate_RendersPercentage()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<GoalValueDisplay>(p =>
                p.Add(x => x.Value, 0.75m)
                 .Add(x => x.DataType, GoalDataType.Boolean));

            Assert.Contains("75 %", cut.Markup);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Scale_RendersValueOverMax()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<GoalValueDisplay>(p =>
                p.Add(x => x.Value, 3.5m)
                 .Add(x => x.DataType, GoalDataType.Scale)
                 .Add(x => x.MaxValue, 5m));

            Assert.Contains("3,5", cut.Markup);
            Assert.Contains("/ 5", cut.Markup);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Percent_RendersValueWithPercent()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            var cut = RenderComponent<GoalValueDisplay>(p =>
                p.Add(x => x.Value, 85m)
                 .Add(x => x.DataType, GoalDataType.Percent));

            Assert.Contains("85 %", cut.Markup);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
