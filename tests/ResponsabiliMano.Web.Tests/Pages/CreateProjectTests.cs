using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
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
        Services.AddSingleton(new LibraryConfiguration { CollocatedJavaScriptQueryString = null });
        Services.AddSingleton<GlobalState>();
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
            model.Goals.Add(new GoalFieldRequest
            {
                Label = "Weight",
                DataType = GoalDataType.Decimal,
                Unit = "kg",
                TargetValue = 70m
            });

            var form = cut.Find("form");
            form.Submit();
        });

        var submitButton = cut.Find("fluent-button[appearance='accent']");
        Assert.True(submitButton.HasAttribute("disabled") || submitButton.HasAttribute("loading"));
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

    // --- Fakes ---

    private sealed class FakeProjectService : IProjectService
    {
        public TaskCompletionSource<Project>? CreateTask { get; set; }

        public Task<Project> CreateProjectAsync(
            Guid creatorId, string name, DateTime startDate, DateTime endDate,
            ProjectFrequency frequency, IEnumerable<GoalFieldInput> goals,
            CancellationToken cancellationToken = default)
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
