using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Pages;

/// <summary>
/// bUnit tests for the account-wide panel (spec S6.1): loading, flag-off, error,
/// empty state, the consolidated counters and one card per project.
/// </summary>
public class GlobalDashboardTests : TestContext
{
    private readonly FakeFeatureManager _featureManager = new();
    private readonly FakeDashboardService _dashboardService = new();
    private readonly FakeAuthStateProvider _authStateProvider = new();

    public GlobalDashboardTests()
    {
        Services.AddSingleton<IDashboardService>(_dashboardService);
        Services.AddSingleton<IFeatureManager>(_featureManager);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
        Services.AddSingleton<ILogger<GlobalDashboard>>(NullLogger<GlobalDashboard>.Instance);
    }

    [Fact]
    public void Renders_loading_state_initially()
    {
        _featureManager.Enabled = true;
        _dashboardService.GlobalResultTask = new TaskCompletionSource<GlobalDashboardResponse>();

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Contains("skeleton", cut.Markup);
    }

    [Fact]
    public void Renders_not_found_when_feature_flag_off()
    {
        _featureManager.Enabled = false;

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Contains("NotFoundTitle", cut.Markup);
        Assert.DoesNotContain("GlobalProjectsSection", cut.Markup);
    }

    [Fact]
    public void Renders_error_message_when_service_throws()
    {
        _featureManager.Enabled = true;
        _dashboardService.GlobalException = new InvalidOperationException("DB down");

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Contains("DashboardError", cut.Markup);
        Assert.Contains("BackToHome", cut.Markup);
    }

    [Fact]
    public void Renders_empty_state_when_user_has_no_projects()
    {
        _featureManager.Enabled = true;
        _dashboardService.GlobalResult = new GlobalDashboardResponse(0, 0, 0, 0, 0, 0, []);

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Contains("GlobalEmptyTitle", cut.Markup);
        Assert.DoesNotContain("GlobalProjectsSection", cut.Markup);
    }

    [Fact]
    public void Renders_consolidated_counters_and_one_card_per_project()
    {
        _featureManager.Enabled = true;
        _dashboardService.GlobalResult = new GlobalDashboardResponse(
            TotalProjects: 2,
            ActiveProjects: 1,
            TotalCheckIns: 9,
            CurrentStreak: 3,
            BestStreak: 5,
            OpenCheckIns: 1,
            Projects:
            [
                Summary("Projeto Verão", ProjectStatus.Active, currentPeriod: 7, totalPeriods: 15, pending: true, feeling: Feeling.Happy),
                Summary("Rumo aos 10K", ProjectStatus.Pending, currentPeriod: 0, totalPeriods: 0, pending: false, feeling: null)
            ]);

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Contains("GlobalDashboardTitle", cut.Markup);
        Assert.Contains("GlobalActiveProjects", cut.Markup);
        Assert.Contains("GlobalTotalCheckIns", cut.Markup);
        Assert.Contains("GlobalOpenCheckIns", cut.Markup);
        Assert.Contains("GlobalStreak", cut.Markup);

        Assert.Contains("Projeto Verão", cut.Markup);
        Assert.Contains("Rumo aos 10K", cut.Markup);
        Assert.Equal(2, cut.FindAll(".gcard").Count);

        // Pending badge and the nudge only show for the project that owes a check-in.
        Assert.Contains("GlobalCheckInPending", cut.Markup);
        Assert.Contains("GlobalOpenCheckInsNudge", cut.Markup);

        // The project that has not started shows the fallback instead of a progress bar.
        Assert.Contains("GlobalNotStarted", cut.Markup);
        Assert.Single(cut.FindAll(".gcard__progress-fill"));
    }

    [Fact]
    public void Compare_link_points_at_the_project_dashboard_for_active_projects_only()
    {
        _featureManager.Enabled = true;
        var active = Summary("Projeto Verão", ProjectStatus.Active, 7, 15, true, Feeling.Happy);
        var pending = Summary("Rumo aos 10K", ProjectStatus.Pending, 0, 0, false, null);
        _dashboardService.GlobalResult = new GlobalDashboardResponse(2, 1, 9, 3, 5, 1, [active, pending]);

        var cut = RenderComponent<GlobalDashboard>();

        Assert.Single(cut.FindAll(".gcard__compare"));
        Assert.Equal(
            $"/projects/{active.ProjectId}/dashboard",
            cut.Find(".gcard__compare").GetAttribute("href"));
    }

    private static GlobalProjectSummary Summary(
        string name,
        ProjectStatus status,
        int currentPeriod,
        int totalPeriods,
        bool pending,
        Feeling? feeling) =>
        new(
            Guid.NewGuid(),
            name,
            "🌊",
            status,
            DateTime.UtcNow.AddDays(-49),
            DateTime.UtcNow.AddDays(56),
            ProjectFrequency.Weekly,
            "Ana Ribeiro",
            "Bruno Tavares",
            currentPeriod,
            totalPeriods,
            7,
            pending,
            feeling,
            4);
}
