using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Web.Components.Design;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Design;

/// <summary>bUnit tests for RmGoalValueInput (spec S7.4).</summary>
public class RmGoalValueInputTests : TestContext
{
    public RmGoalValueInputTests()
    {
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
    }

    [Fact]
    public void Boolean_RendersYesNoButtons()
    {
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Boolean)
             .Add(x => x.Value, 1m));

        var buttons = cut.FindAll(".rm-segmented__item");
        Assert.Equal(2, buttons.Count);
        Assert.Contains("BooleanYes", cut.Markup);
        Assert.Contains("BooleanNo", cut.Markup);
    }

    [Fact]
    public void Boolean_ClickingNo_FiresValueChanged()
    {
        decimal? boundValue = null;
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Boolean)
             .Add(x => x.Value, 1m)
             .Add(x => x.ValueChanged, (decimal? v) => boundValue = v));

        cut.FindAll(".rm-segmented__item").ElementAt(1).Click();

        Assert.Equal(0m, boundValue);
    }

    [Fact]
    public void Scale_RendersSegmentedButtonsForRange()
    {
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Scale)
             .Add(x => x.MinValue, 1m)
             .Add(x => x.MaxValue, 5m)
             .Add(x => x.Value, 3m));

        var buttons = cut.FindAll(".rm-segmented__item");
        Assert.Equal(5, buttons.Count);
        Assert.All(buttons, b => Assert.Contains(b.TextContent.Trim(), "12345"));
    }

    [Fact]
    public void Scale_ClickingButton_FiresValueChanged()
    {
        decimal? boundValue = null;
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Scale)
             .Add(x => x.MinValue, 1m)
             .Add(x => x.MaxValue, 5m)
             .Add(x => x.ValueChanged, (decimal? v) => boundValue = v));

        cut.FindAll(".rm-segmented__item").ElementAt(3).Click();

        Assert.Equal(4m, boundValue);
    }

    [Fact]
    public void Integer_RendersStepperWithNumberInput()
    {
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Integer)
             .Add(x => x.Value, 5m));

        Assert.NotNull(cut.Find("input.rm-input"));
        Assert.Equal(2, cut.FindAll(".rm-stepper__button").Count);
    }

    [Fact]
    public void Percent_RendersSliderAndNumberInput()
    {
        var cut = RenderComponent<RmGoalValueInput>(p =>
            p.Add(x => x.DataType, GoalDataType.Percent)
             .Add(x => x.Value, 50m));

        Assert.NotNull(cut.Find("input.rm-slider"));
        Assert.NotNull(cut.Find("input.rm-input"));
    }
}
