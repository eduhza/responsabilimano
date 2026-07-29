using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;

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
        Services.AddSingleton(new LibraryConfiguration { CollocatedJavaScriptQueryString = null });
        Services.AddSingleton<GlobalState>();
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
        Assert.DoesNotContain("DashboardTitle", cut.Markup);
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
            [
                new DashboardParticipant(user1Id, "Alice", Feeling.Happy),
                new DashboardParticipant(user2Id, "Bob", null)
            ],
            [
                new DashboardMetricSeries(
                    goalId, "Weight", "kg", GoalDataType.Decimal, 70m,
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
            [new DashboardParticipant(user1Id, "Alice", Feeling.Neutral)],
            [
                new DashboardMetricSeries(goal1Id, "Weight", "kg", GoalDataType.Decimal, null, []),
                new DashboardMetricSeries(goal2Id, "Sleep", "h", GoalDataType.Integer, null, [])
            ]);

        var cut = RenderComponent<Dashboard>(p => p.Add(x => x.ProjectId, projectId));

        Assert.Contains("DashboardGoalSelector", cut.Markup);
        Assert.NotNull(cut.Find("fluent-select"));
        Assert.Equal(2, cut.FindAll("fluent-option").Count);
    }

    // --- Fakes ---

    private sealed class FakeProjectService : IProjectService
    {
        public Task<(int Current, int Best)> GetStreakAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult((0, 0));

        public Task<Project> CreateProjectAsync(
            Guid creatorId, string name, DateTime startDate, DateTime endDate,
            ProjectFrequency frequency, IEnumerable<GoalFieldInput> goals,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ProjectInvitation> InvitePartnerAsync(
            Guid projectId, Guid inviterUserId, string partnerEmail, string baseUrl,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Project?> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Project?> GetInvitationProjectAsync(string token, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Project?> GetProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<Project>> GetUserProjectsAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task ApproveProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ProjectChangeRequest> ProposeChangeAsync(
            Guid projectId, Guid userId, ChangeRequestType type, string payloadJson,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task RespondToChangeRequestAsync(
            Guid projectId, Guid changeRequestId, Guid userId, bool approve,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeFeatureManager : IFeatureManager
    {
        public bool Enabled { get; set; }
        public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(Enabled);
        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) where TContext : notnull => Task.FromResult(Enabled);
        public IAsyncEnumerable<string> GetFeatureNamesAsync() => AsyncEnumerable.Empty<string>();
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public DashboardResponse? Result { get; set; }
        public TaskCompletionSource<DashboardResponse?>? ResultTask { get; set; }
        public Exception? Exception { get; set; }

        public Task<DashboardResponse?> GetDashboardAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (Exception is not null) return Task.FromException<DashboardResponse?>(Exception);
            if (ResultTask is not null) return ResultTask.Task;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,
                    "00000000-0000-0000-0000-000000000001")],
                "TestAuth");
            return Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity)));
        }
    }

    private sealed class FakeJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (typeof(TValue) == typeof(IJSObjectReference))
                return new ValueTask<TValue>((TValue)(object)new FakeJSModuleReference());
            return default;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (typeof(TValue) == typeof(IJSObjectReference))
                return new ValueTask<TValue>((TValue)(object)new FakeJSModuleReference());
            return default;
        }
    }

    private sealed class FakeJSModuleReference : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
        public ValueTask InvokeVoidAsync(string identifier, object?[]? args) => default;
        public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
        public ValueTask DisposeAsync() => default;
    }

    private sealed class PassthroughLocalizer : IStringLocalizer<AppStrings>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
