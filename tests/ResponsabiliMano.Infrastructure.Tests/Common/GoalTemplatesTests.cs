using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Tests.Common;

/// <summary>
/// Guards the static goal-template catalog (spec S7.5). Every template must be valid
/// according to the same rules the server enforces in <see cref="GoalValueRules"/>, so
/// nobody can add a broken model to the catalog without a failing test.
/// </summary>
public class GoalTemplatesTests
{
    [Fact]
    public void Catalog_has_at_least_five_templates()
    {
        Assert.True(GoalTemplates.All.Count >= 5);
    }

    [Fact]
    public void Every_template_has_unique_nonempty_key_name_and_icon()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in GoalTemplates.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Key), $"Template '{template.Name}' has empty key.");
            Assert.False(string.IsNullOrWhiteSpace(template.Name), $"Template '{template.Key}' has empty name.");
            Assert.False(string.IsNullOrWhiteSpace(template.Icon), $"Template '{template.Key}' has empty icon.");
            Assert.True(keys.Add(template.Key), $"Duplicate template key '{template.Key}'.");
        }
    }

    [Theory]
    [MemberData(nameof(AllTemplates))]
    public void Every_template_has_between_three_and_five_goals(GoalTemplate template)
    {
        Assert.InRange(template.Goals.Count, 3, 5);
    }

    [Theory]
    [MemberData(nameof(AllTemplateGoals))]
    public void Every_goal_is_valid_according_to_server_rules(GoalTemplate template, GoalTemplateGoal goal)
    {
        Assert.Contains(goal, template.Goals);

        // The definition (bounds) must be valid for the data type.
        var definitionError = GoalValueRules.ValidateDefinition(goal.DataType, goal.Min, goal.Max);
        Assert.Null(definitionError);

        // The per-participant target must be valid too — this is exactly what
        // CreateProjectAsync runs before saving.
        var targetError = GoalValueRules.ValidateTarget(
            goal.DataType, goal.Min, goal.Max, goal.Baseline, goal.TargetValue, goal.Direction);
        Assert.Null(targetError);
    }

    [Theory]
    [MemberData(nameof(AllTemplateGoals))]
    public void Boolean_goals_are_fixed_to_0_1_and_reach(GoalTemplate template, GoalTemplateGoal goal)
    {
        Assert.Contains(goal, template.Goals);
        if (goal.DataType != GoalDataType.Boolean)
            return;

        Assert.Equal(0m, goal.Min);
        Assert.Equal(1m, goal.Max);
        Assert.Equal(GoalDirection.Reach, goal.Direction);
        Assert.Equal(string.Empty, goal.Unit);
        Assert.Equal(1m, goal.TargetValue);
    }

    [Theory]
    [MemberData(nameof(AllTemplateGoals))]
    public void Scale_goals_have_integer_bounds_with_max_greater_than_min(GoalTemplate template, GoalTemplateGoal goal)
    {
        Assert.Contains(goal, template.Goals);
        if (goal.DataType != GoalDataType.Scale)
            return;

        Assert.NotNull(goal.Min);
        Assert.NotNull(goal.Max);
        Assert.Equal(decimal.Truncate(goal.Min!.Value), goal.Min);
        Assert.Equal(decimal.Truncate(goal.Max!.Value), goal.Max);
        Assert.True(goal.Max > goal.Min, $"Scale goal '{goal.Label}' has max <= min.");
        Assert.True(goal.Direction is GoalDirection.Increase or GoalDirection.Reach);
    }

    [Theory]
    [MemberData(nameof(AllTemplateGoals))]
    public void Percent_goals_are_fixed_to_0_100(GoalTemplate template, GoalTemplateGoal goal)
    {
        Assert.Contains(goal, template.Goals);
        if (goal.DataType != GoalDataType.Percent)
            return;

        Assert.Equal(0m, goal.Min);
        Assert.Equal(100m, goal.Max);
        Assert.False(string.IsNullOrWhiteSpace(goal.Unit));
    }

    [Theory]
    [MemberData(nameof(AllTemplateGoals))]
    public void Non_boolean_goals_have_a_nonempty_unit(GoalTemplate template, GoalTemplateGoal goal)
    {
        Assert.Contains(goal, template.Goals);
        if (goal.DataType == GoalDataType.Boolean)
            return;

        Assert.False(string.IsNullOrWhiteSpace(goal.Unit), $"Goal '{goal.Label}' ({goal.DataType}) has empty unit.");
    }

    [Fact]
    public void Concurso_template_has_four_goals_with_expected_types()
    {
        var concurso = GoalTemplates.Find("concurso");
        Assert.NotNull(concurso);

        Assert.Equal(4, concurso!.Goals.Count);
        Assert.Equal(GoalDataType.Decimal, concurso.Goals[0].DataType);
        Assert.Equal(GoalDataType.Integer, concurso.Goals[1].DataType);
        Assert.Equal(GoalDataType.Boolean, concurso.Goals[2].DataType);
        Assert.Equal(GoalDataType.Scale, concurso.Goals[3].DataType);
    }

    [Fact]
    public void Find_returns_null_for_unknown_key()
    {
        Assert.Null(GoalTemplates.Find("does-not-exist"));
    }

    public static TheoryData<GoalTemplate> AllTemplates
    {
        get
        {
            var data = new TheoryData<GoalTemplate>();
            foreach (var t in GoalTemplates.All)
                data.Add(t);
            return data;
        }
    }

    public static TheoryData<GoalTemplate, GoalTemplateGoal> AllTemplateGoals
    {
        get
        {
            var data = new TheoryData<GoalTemplate, GoalTemplateGoal>();
            foreach (var t in GoalTemplates.All)
                foreach (var g in t.Goals)
                    data.Add(t, g);
            return data;
        }
    }
}
