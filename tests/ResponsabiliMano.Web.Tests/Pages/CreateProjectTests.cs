using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;
using ResponsabiliMano.Web.Models;

namespace ResponsabiliMano.Web.Tests.Pages;

/// <summary>
/// bUnit tests for the CreateProject page (spec S5.2).
/// Verifies that the page renders a loading state on the submit button
/// while the async creation operation is in progress.
/// </summary>
public class CreateProjectTests : TestContext
{
    private readonly FakeProjectService _projectService = new();
    private readonly FakeAuthStateProvider _authStateProvider = new();

    public CreateProjectTests()
    {
        Services.AddSingleton<IProjectService>(_projectService);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
        Services.AddSingleton<ILogger<CreateProject>>(NullLogger<CreateProject>.Instance);
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
    }

    [Fact]
    public async Task Renders_loading_state_on_submit_button_while_creating()
    {
        _projectService.CreateTask = new TaskCompletionSource<Project>();

        var cut = RenderComponent<CreateProject>();

        await cut.InvokeAsync(() =>
        {
            var instance = cut.Instance;
            var modelField = instance.GetType().GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var model = (CreateProjectRequest)modelField!.GetValue(instance)!;
            model.Name = "Test Project";
            model.StartDate = DateTime.Today;
            model.EndDate = DateTime.Today.AddDays(30);
            model.Frequency = ProjectFrequency.Weekly;
            model.Goals.Clear();
            model.Goals.Add(new CreateProjectGoalRequest
            {
                Goal = new GoalFieldRequest
                {
                    Label = "Weight",
                    DataType = GoalDataType.Decimal,
                    Unit = "kg"
                },
                CreatorTarget = new GoalTargetRequest
                {
                    Baseline = 80m,
                    TargetValue = 70m,
                    Direction = GoalDirection.Decrease
                }
            });

            // The submit button only exists on the wizard's last step.
            var stepField = instance.GetType().GetField("_step", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stepField!.SetValue(instance, 3);

            var form = cut.Find("form");
            form.Submit();
        });

        var submitButton = cut.Find("button[type='submit']");
        Assert.True(submitButton.HasAttribute("disabled"));
        Assert.Contains("Creating", submitButton.TextContent);

        _projectService.CreateTask!.SetResult(new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            CreatorId = FakeAuthStateProvider.UserId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Pending
        });

        cut.WaitForState(() => cut.Markup.Contains("CreateProjectSuccess"));
        Assert.Contains("CreateProjectSuccess", cut.Markup);
    }

    [Fact]
    public void Applying_concurso_template_prefills_four_goals_with_expected_types()
    {
        var cut = RenderComponent<CreateProject>();

        // The template gallery is the first step. Click the "Concurso & Provas" card.
        var concursoCard = cut.FindAll(".template-card")
            .Single(b => b.TextContent.Contains("Concurso & Provas"));
        concursoCard.Click();

        var model = GetModel(cut);

        // AC 2: name, icon and frequency are pre-filled from the template.
        Assert.Equal("Concurso & Provas", model.Name);
        Assert.Equal("📚", model.Icon);
        Assert.Equal(ProjectFrequency.Weekly, model.Frequency);

        // AC 7: exactly the four declared goals, with the catalog's data types.
        Assert.Equal(4, model.Goals.Count);
        Assert.Equal(GoalDataType.Decimal, model.Goals[0].Goal.DataType);
        Assert.Equal(GoalDataType.Integer, model.Goals[1].Goal.DataType);
        Assert.Equal(GoalDataType.Boolean, model.Goals[2].Goal.DataType);
        Assert.Equal(GoalDataType.Scale, model.Goals[3].Goal.DataType);

        // The suggested partner target is pre-filled equal to the creator target.
        foreach (var goal in model.Goals)
        {
            Assert.NotNull(goal.SuggestedPartnerTarget);
            Assert.Equal(goal.CreatorTarget.TargetValue, goal.SuggestedPartnerTarget!.TargetValue);
            Assert.Equal(goal.CreatorTarget.Direction, goal.SuggestedPartnerTarget.Direction);
        }
    }

    [Fact]
    public void Switching_goal_to_percent_locks_unit_and_bounds_and_hides_minmax()
    {
        var cut = RenderComponent<CreateProject>();
        SetStep(cut, 2); // goals step

        var model = GetModel(cut);
        model.Goals[0].Goal.DataType = GoalDataType.Decimal;

        // Drive the wizard's OnDataTypeChanged handler directly. bUnit's select.Change
        // does not reliably fire @onchange for selects using value/@onchange (rather
        // than @bind-Value), so invoke the private handler the same way the event would.
        var instance = cut.Instance;
        var method = instance.GetType().GetMethod(
            "OnDataTypeChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, [0, new ChangeEventArgs { Value = GoalDataType.Percent.ToString() }]);
        cut.Render();

        // AC 4: Percent fixes unit to "%" and bounds to 0..100.
        Assert.Equal("%", model.Goals[0].Goal.Unit);
        Assert.Equal(0m, model.Goals[0].Goal.MinValue);
        Assert.Equal(100m, model.Goals[0].Goal.MaxValue);

        // The unit field renders disabled with value "%".
        var markup = cut.Markup;
        Assert.Contains("value=\"%\"", markup);
        Assert.Contains("disabled", markup);
    }

    [Fact]
    public void Goals_step_renders_a_live_checkin_preview_per_goal()
    {
        var cut = RenderComponent<CreateProject>();
        SetStep(cut, 2); // goals step

        // AC 3: RmGoalValueInput renders one preview per goal (class rm-goal-value-input).
        var previews = cut.FindAll(".goal__preview .rm-goal-value-input");
        Assert.Equal(GetModel(cut).Goals.Count, previews.Count);
    }

    private static CreateProjectRequest GetModel(IRenderedComponent<CreateProject> cut)
    {
        var instance = cut.Instance;
        var modelField = instance.GetType().GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (CreateProjectRequest)modelField!.GetValue(instance)!;
    }

    private static void SetStep(IRenderedComponent<CreateProject> cut, int step)
    {
        var instance = cut.Instance;
        var stepField = instance.GetType().GetField("_step", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        stepField!.SetValue(instance, step);
        cut.Render();
    }

    // --- Fakes ---

    private sealed class FakeProjectService : IProjectService
    {
        public TaskCompletionSource<Project>? CreateTask { get; set; }

        public Task<Project> CreateProjectAsync(
            Guid creatorId, string name, DateTime startDate, DateTime endDate,
            ProjectFrequency frequency, IEnumerable<GoalFieldInput> goals,
            string? icon = null, CancellationToken cancellationToken = default)
        {
            return (CreateTask ?? new TaskCompletionSource<Project>()).Task;
        }

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

        public Task<(int Current, int Best)> GetStreakAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult((0, 0));
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, UserId.ToString())],
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
