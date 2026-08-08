using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Pages;

/// <summary>
/// bUnit tests for the Dashboard page (spec S4.2).
/// Covers loading state, error state, flag-off (404) state, and successful render.
/// </summary>
public class DashboardTests : TestContext
{
    private readonly FakeFeatureManager _featureManager = new();
    private readonly FakeDashboardService _dashboardService = new();
    private readonly FakeProjectService _projectService = new();
    private readonly FakeAuthStateProvider _authStateProvider = new();
    private readonly FakeJSRuntime _jsRuntime = new();

    public DashboardTests()
    {
        Services.AddSingleton<IDashboardService>(_dashboardService);
        Services.AddSingleton<IProjectService>(_projectService);
        Services.AddSingleton<IFeatureManager>(_featureManager);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
        Services.AddSingleton<IJSRuntime>(_jsRuntime);
        Services.AddSingleton<ILogger<Dashboard>>(NullLogger<Dashboard>.Instance);
    }

    [Fact]
    public void Renders_loading_state_initially()
    {
        _featureManager.Enabled = true;
        _dashboardService.ResultTask = new TaskCompletionSource<DashboardResponse?>();

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, Guid.NewGuid()));

        Assert.Contains("skeleton", cut.Markup);
    }

    [Fact]
    public void Renders_not_found_when_feature_flag_off()
    {
        _featureManager.Enabled = false;

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, Guid.NewGuid()));

        Assert.Contains("ProjectNotFound", cut.Markup);
        Assert.DoesNotContain("ProjectDashboardTitle", cut.Markup);
    }

    [Fact]
    public void Renders_not_found_when_project_does_not_exist()
    {
        _featureManager.Enabled = true;
        _dashboardService.Result = null;

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, Guid.NewGuid()));

        Assert.Contains("ProjectNotFound", cut.Markup);
    }

    [Fact]
    public void Renders_error_message_when_service_throws()
    {
        _featureManager.Enabled = true;
        _dashboardService.Exception = new InvalidOperationException("DB down");

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, Guid.NewGuid()));

        Assert.Contains("DashboardError", cut.Markup);
        Assert.Contains("BackToProject", cut.Markup);
    }

    [Fact]
    public void Renders_not_found_when_user_not_participant()
    {
        _featureManager.Enabled = true;
        _dashboardService.Exception = new UnauthorizedAccessException();

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, Guid.NewGuid()));

        Assert.Contains("ProjectNotFound", cut.Markup);
    }

    [Fact]
    public void Renders_dashboard_content_on_success()
    {
        var projectId = Guid.NewGuid();
        var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var user2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var goalId = Guid.NewGuid();

        _featureManager.Enabled = true;
        _dashboardService.Result = new DashboardResponse(
            projectId,
            "Diet Project",
            2,
            10,
            [
                new DashboardParticipant(user1Id, "Alice", Feeling.Happy),
                new DashboardParticipant(user2Id, "Bob", null)
            ],
            [
                new DashboardMetricSeries(
                    goalId, "Weight", "kg", GoalDataType.Decimal, 40m, 120m,
                    [
                        new DashboardMetricTarget(user1Id, null, 70m, GoalDirection.Reach),
                        new DashboardMetricTarget(user2Id, null, 70m, GoalDirection.Reach)
                    ],
                    [
                        new DashboardSeriesEntry(user1Id, 1, DateTime.UtcNow, 80m, 79m),
                        new DashboardSeriesEntry(user1Id, 2, DateTime.UtcNow, 78m, 79m),
                        new DashboardSeriesEntry(user2Id, 1, DateTime.UtcNow, 75m, 74m),
                        new DashboardSeriesEntry(user2Id, 2, DateTime.UtcNow, 73m, 74m)
                    ])
            ]);

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, projectId));

        Assert.Contains("Diet Project", cut.Markup);
        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("Bob", cut.Markup);
        Assert.Contains("DashboardNoCheckIns", cut.Markup);
        Assert.Contains("BackToProject", cut.Markup);
        Assert.Contains("DashboardAverages", cut.Markup);
        // Project-scoped framing (spec S6.1): comparison title plus this project's
        // progress, and none of the account-wide counters.
        Assert.Contains("ProjectDashboardTitle", cut.Markup);
        Assert.Contains("ProjectDashboardProgress", cut.Markup);
        Assert.DoesNotContain("GlobalDashboardTitle", cut.Markup);
    }

    [Fact]
    public void Renders_goal_selector_when_multiple_goals()
    {
        var projectId = Guid.NewGuid();
        var user1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var goal1Id = Guid.NewGuid();
        var goal2Id = Guid.NewGuid();

        _featureManager.Enabled = true;
        _dashboardService.Result = new DashboardResponse(
            projectId,
            "Multi Goal Project",
            1,
            8,
            [new DashboardParticipant(user1Id, "Alice", Feeling.Neutral)],
            [
                new DashboardMetricSeries(goal1Id, "Weight", "kg", GoalDataType.Decimal, 40m, 120m, [], []),
                new DashboardMetricSeries(goal2Id, "Sleep", "h", GoalDataType.Integer, 0m, 24m, [], [])
            ]);

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, projectId));

        Assert.Contains("DashboardGoalSelector", cut.Markup);
        // The goal picker is a segmented control now, one button per metric.
        Assert.NotNull(cut.Find(".rm-segmented"));
        Assert.Equal(2, cut.FindAll(".rm-segmented__item").Count);
    }

}
