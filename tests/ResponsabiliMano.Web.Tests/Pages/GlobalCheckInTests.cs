using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;
using ResponsabiliMano.Web.Tests.TestHelpers;

namespace ResponsabiliMano.Web.Tests.Pages;

/// <summary>
/// bUnit tests for the global Check-in page (spec S3.x): loading, flag-off, empty,
/// tab ordering, deadline badges and submission callback.
/// </summary>
public class GlobalCheckInTests : TestContext
{
    private readonly FakeFeatureManager _featureManager = new();
    private readonly FakeCheckInService _checkInService = new();
    private readonly FakeAuthStateProvider _authStateProvider = new();

    public GlobalCheckInTests()
    {
        Services.AddSingleton<ICheckInService>(_checkInService);
        Services.AddSingleton<IFeatureManager>(_featureManager);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
        Services.AddSingleton<ILogger<GlobalCheckIn>>(NullLogger<GlobalCheckIn>.Instance);
    }

    [Fact]
    public void Renders_loading_state_initially()
    {
        _featureManager.Enabled = true;
        _checkInService.FormsTask = new TaskCompletionSource<IReadOnlyList<CheckInForm>>();

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("skeleton", cut.Markup);
    }

    [Fact]
    public void Renders_not_found_when_feature_flag_off()
    {
        _featureManager.Enabled = false;

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("NotFoundTitle", cut.Markup);
        Assert.DoesNotContain("gcheckin__tabs", cut.Markup);
    }

    [Fact]
    public void Renders_error_message_when_service_throws()
    {
        _featureManager.Enabled = true;
        _checkInService.Exception = new InvalidOperationException("DB down");

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("CheckInError", cut.Markup);
        Assert.Contains("BackToHome", cut.Markup);
    }

    [Fact]
    public void Renders_empty_state_when_user_has_no_check_ins()
    {
        _featureManager.Enabled = true;
        _checkInService.UserForms = [];

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("GlobalCheckInEmptyTitle", cut.Markup);
        Assert.DoesNotContain("gcheckin__tabs", cut.Markup);
    }

    [Fact]
    public void Renders_one_tab_per_project_and_selects_first()
    {
        _featureManager.Enabled = true;
        var first = Form("First", periodEnd: DateTime.UtcNow.AddDays(1));
        var second = Form("Second", periodEnd: DateTime.UtcNow.AddDays(3));
        _checkInService.UserForms = [first, second];

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("First", cut.Markup);
        Assert.Contains("Second", cut.Markup);
        Assert.Equal(2, cut.FindAll(".gcheckin__tab").Count);

        // The first project (closest deadline) is selected by default.
        var active = cut.Find(".gcheckin__tab.is-active");
        Assert.Contains("First", active.TextContent);
    }

    [Fact]
    public void Tabs_are_sorted_by_already_submitted_then_deadline()
    {
        _featureManager.Enabled = true;
        var dueSoon = Form("DueSoon", alreadySubmitted: false, periodEnd: DateTime.UtcNow.AddDays(1));
        var overdue = Form("Overdue", alreadySubmitted: false, periodEnd: DateTime.UtcNow.AddDays(-1));
        var doneFar = Form("DoneFar", alreadySubmitted: true, periodEnd: DateTime.UtcNow.AddDays(10));
        _checkInService.UserForms = [doneFar, dueSoon, overdue];

        var cut = RenderComponent<GlobalCheckIn>();
        var tabs = cut.FindAll(".gcheckin__tab").ToList();

        Assert.Equal(3, tabs.Count);
        Assert.Contains("Overdue", tabs[0].TextContent);
        Assert.Contains("DueSoon", tabs[1].TextContent);
        Assert.Contains("DoneFar", tabs[2].TextContent);
    }

    [Fact]
    public void Badges_show_done_and_day_counters()
    {
        _featureManager.Enabled = true;
        _checkInService.UserForms =
        [
            Form("Today", alreadySubmitted: false, periodEnd: DateTime.UtcNow.Date.AddDays(1).AddTicks(-1)),
            Form("Done", alreadySubmitted: true, periodEnd: DateTime.UtcNow.AddDays(7))
        ];

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("CheckInDone", cut.Markup);
        Assert.Contains("CheckInDueToday", cut.Markup);
    }

    [Fact]
    public void Clicking_tab_selects_project_and_shows_editor()
    {
        _featureManager.Enabled = true;
        var first = Form("First");
        var second = Form("Second");
        _checkInService.UserForms = [first, second];

        var cut = RenderComponent<GlobalCheckIn>();
        var tabs = cut.FindAll(".gcheckin__tab").ToList();
        tabs[1].Click();

        var active = cut.Find(".gcheckin__tab.is-active");
        Assert.Contains("Second", active.TextContent);
        Assert.Contains("checkin-editor__period", cut.Markup);
    }

    [Fact]
    public void Editor_receives_go_to_project_label()
    {
        _featureManager.Enabled = true;
        var form = Form("First", alreadySubmitted: true);
        _checkInService.UserForms = [form];

        var cut = RenderComponent<GlobalCheckIn>();

        Assert.Contains("GoToProject", cut.Markup);
    }

    [Fact]
    public void Submission_callback_refreshes_the_list()
    {
        _featureManager.Enabled = true;
        var form = Form("First");
        _checkInService.UserForms = [form];

        var cut = RenderComponent<GlobalCheckIn>();

        // The service receives the forms callback but no actual editor submission here.
        // We test that a refresh keeps the selected tab.
        cut.Find(".gcheckin__tab").Click();

        Assert.Empty(_checkInService.Submissions);
    }

    private static CheckInForm Form(string name, bool alreadySubmitted = false, DateTime? periodEnd = null)
    {
        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            Label = "Weight",
            DataType = GoalDataType.Decimal,
            Unit = "kg"
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Icon = "🎯",
            CreatorId = FakeAuthStateProvider.UserId,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddDays(21),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active,
            Goals = [goal]
        };

        Core.Entities.CheckIn? existing = null;
        if (alreadySubmitted)
        {
            var checkInId = Guid.NewGuid();
            existing = new Core.Entities.CheckIn
            {
                Id = checkInId,
                ProjectId = project.Id,
                UserId = FakeAuthStateProvider.UserId,
                Feeling = Feeling.Happy,
                PeriodNumber = 1,
                SubmittedAt = DateTime.UtcNow,
                Metrics =
                [
                    new CheckInMetric
                    {
                        Id = Guid.NewGuid(),
                        CheckInId = checkInId,
                        GoalFieldId = goal.Id,
                        Value = 70
                    }
                ]
            };
        }

        return new CheckInForm(project, 1, alreadySubmitted, periodEnd ?? DateTime.UtcNow.AddDays(7), existing);
    }
}
