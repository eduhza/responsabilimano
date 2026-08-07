using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using Microsoft.JSInterop;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Tests.TestHelpers;

/// <summary>Flag stub: every feature answers with <see cref="Enabled"/>.</summary>
internal sealed class FakeFeatureManager : IFeatureManager
{
    public bool Enabled { get; set; }
    public Task<bool> IsEnabledAsync(string feature) => Task.FromResult(Enabled);
    public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context) where TContext : notnull => Task.FromResult(Enabled);
    public IAsyncEnumerable<string> GetFeatureNamesAsync() => AsyncEnumerable.Empty<string>();
}

/// <summary>
/// Dashboard stub. <see cref="ResultTask"/> lets a test hold the call open to observe
/// the loading state; <see cref="Exception"/> makes it fault.
/// </summary>
internal sealed class FakeDashboardService : IDashboardService
{
    public DashboardResponse? Result { get; set; }
    public TaskCompletionSource<DashboardResponse?>? ResultTask { get; set; }
    public Exception? Exception { get; set; }

    public GlobalDashboardResponse? GlobalResult { get; set; }
    public TaskCompletionSource<GlobalDashboardResponse>? GlobalResultTask { get; set; }
    public Exception? GlobalException { get; set; }

    public Task<DashboardResponse?> GetDashboardAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (Exception is not null) return Task.FromException<DashboardResponse?>(Exception);
        if (ResultTask is not null) return ResultTask.Task;
        return Task.FromResult(Result);
    }

    public Task<GlobalDashboardResponse> GetGlobalDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (GlobalException is not null) return Task.FromException<GlobalDashboardResponse>(GlobalException);
        if (GlobalResultTask is not null) return GlobalResultTask.Task;
        return Task.FromResult(GlobalResult ?? new GlobalDashboardResponse(0, 0, 0, 0, 0, 0, []));
    }
}

/// <summary>Only the members the pages under test actually call are implemented.</summary>
internal sealed class FakeProjectService : IProjectService
{
    public (int Current, int Best) Streak { get; set; }

    public Task<(int Current, int Best)> GetStreakAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Streak);

    public Task<Project> CreateProjectAsync(
        Guid creatorId, string name, DateTime startDate, DateTime endDate,
        ProjectFrequency frequency, IEnumerable<GoalFieldInput> goals,
        string? icon = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<ProjectInvitation> InvitePartnerAsync(
        Guid projectId, Guid inviterUserId, string partnerEmail, string baseUrl,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Project?> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<Project?> GetInvitationProjectAsync(string token, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Project? Result { get; set; }

    public Task<Project?> GetProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result);

    public Task<List<Project>> GetUserProjectsAsync(Guid userId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task ApproveProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<ProjectChangeRequest> ProposeChangeAsync(
        Guid projectId, Guid userId, ChangeRequestType type, string payloadJson,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RespondToChangeRequestAsync(
        Guid projectId, Guid changeRequestId, Guid userId, bool approve,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

/// <summary>Signs the tests in as user <c>...0001</c>.</summary>
internal sealed class FakeAuthStateProvider : AuthenticationStateProvider
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

internal sealed class FakeJSRuntime : IJSRuntime
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

internal sealed class FakeJSModuleReference : IJSObjectReference
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    public ValueTask InvokeVoidAsync(string identifier, object?[]? args) => default;
    public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args) => default;
    public ValueTask DisposeAsync() => default;
}

/// <summary>Check-in stub. Allows tests to set forms for the global page and
/// tracks submissions.</summary>
internal sealed class FakeCheckInService : ICheckInService
{
    public CheckInForm? Form { get; set; }
    public List<CheckInForm> UserForms { get; set; } = [];
    public TaskCompletionSource<IReadOnlyList<CheckInForm>>? FormsTask { get; set; }
    public Exception? Exception { get; set; }
    public List<(Guid ProjectId, Guid UserId, Feeling Feeling, IReadOnlyCollection<CheckInMetricInput> Metrics)> Submissions { get; } = [];
    public List<(Guid ProjectId, Guid UserId, Feeling Feeling, IReadOnlyCollection<CheckInMetricInput> Metrics)> Updates { get; } = [];
    public List<(Guid ProjectId, Guid UserId)> Deletions { get; } = [];

    public Task<CheckInForm?> GetCheckInFormAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (Exception is not null) return Task.FromException<CheckInForm?>(Exception);
        return Task.FromResult(Form);
    }

    public Task<IReadOnlyList<CheckInForm>> GetCheckInFormsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (Exception is not null) return Task.FromException<IReadOnlyList<CheckInForm>>(Exception);
        if (FormsTask is not null) return FormsTask.Task;
        return Task.FromResult<IReadOnlyList<CheckInForm>>(UserForms);
    }

    public Task<CheckIn> SubmitCheckInAsync(
        Guid projectId,
        Guid userId,
        Feeling feeling,
        IReadOnlyCollection<CheckInMetricInput> metrics,
        CancellationToken cancellationToken = default)
    {
        Submissions.Add((projectId, userId, feeling, metrics));
        return Task.FromResult(new CheckIn
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = feeling,
            PeriodNumber = 1,
            SubmittedAt = DateTime.UtcNow
        });
    }

    public Task<CheckIn?> UpdateCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        Feeling feeling,
        IReadOnlyCollection<CheckInMetricInput> metrics,
        CancellationToken cancellationToken = default)
    {
        if (Exception is not null) return Task.FromException<CheckIn?>(Exception);
        Updates.Add((projectId, userId, feeling, metrics));
        return Task.FromResult<CheckIn?>(new CheckIn
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = feeling,
            PeriodNumber = 1,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Task<bool> DeleteCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (Exception is not null) return Task.FromException<bool>(Exception);
        Deletions.Add((projectId, userId));
        return Task.FromResult(true);
    }
}

/// <summary>
/// Echoes resource keys instead of translations, so assertions can target the key
/// and stay independent of the copy.
/// </summary>
internal sealed class PassthroughLocalizer : IStringLocalizer<AppStrings>
{
    public LocalizedString this[string name] => new(name, name);
    public LocalizedString this[string name, params object[] arguments] => new(name, name);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
