using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.E2ETests;

[Collection("Integration")]
public class CronAndEmailFlowTests : IAsyncLifetime
{
    private readonly ResponsabiliManoWebApp _app;
    private readonly HttpClient _httpClient;

    public CronAndEmailFlowTests(ResponsabiliManoWebApp app)
    {
        _app = app;
        _httpClient = new HttpClient { BaseAddress = new Uri(app.BaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("X-Cron-Secret", "test-cron-secret");
    }

    public Task InitializeAsync() => _app.ResetDatabaseAsync();
    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Cron_dispatches_check_in_emails_and_idempotent()
    {
        var (projectId, _, _) = await SeedActiveProjectAsync();

        // Ignore the invitation email; only check-in dispatch emails count.
        _app.CapturedEmails.Clear();

        var dispatchResponse = await _httpClient.PostAsync("/api/cron/checkins/dispatch", null);
        dispatchResponse.EnsureSuccessStatusCode();
        var dispatchResult = await dispatchResponse.Content.ReadFromJsonAsync<DispatchResult>();
        Assert.Equal(2, dispatchResult!.Sent);

        var emails = _app.CapturedEmails.GetAll();
        Assert.Equal(2, emails.Count);
        Assert.All(emails, e => Assert.Contains("Hora do check-in", e.Subject));

        // Idempotency: running again in the same period sends nothing.
        var secondResponse = await _httpClient.PostAsync("/api/cron/checkins/dispatch", null);
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<DispatchResult>();
        Assert.Equal(0, secondResult!.Sent);
    }

    [Fact]
    public async Task Reminders_sent_only_to_participants_without_check_in_and_are_idempotent()
    {
        var (projectId, creatorId, partnerId) = await SeedActiveProjectAsync();

        // Ignore the invitation email; only reminder emails count.
        _app.CapturedEmails.Clear();

        // Creator fills check-in; only partner should remain pending.
        await SubmitCheckInAsync(projectId, creatorId, 70);

        // First run: only the partner receives a reminder.
        var reminderResponse = await _httpClient.PostAsync("/api/cron/checkins/reminders", null);
        reminderResponse.EnsureSuccessStatusCode();
        var reminderResult = await reminderResponse.Content.ReadFromJsonAsync<DispatchResult>();
        Assert.Equal(1, reminderResult!.Sent);

        var emails = _app.CapturedEmails.GetAll();
        var email = Assert.Single(emails);
        Assert.Contains("Lembrete de check-in", email.Subject);

        // Partner also fills check-in; no one remains pending.
        await SubmitCheckInAsync(projectId, partnerId, 70);

        var secondReminderResponse = await _httpClient.PostAsync("/api/cron/checkins/reminders", null);
        secondReminderResponse.EnsureSuccessStatusCode();
        var secondReminderResult = await secondReminderResponse.Content.ReadFromJsonAsync<DispatchResult>();
        Assert.Equal(0, secondReminderResult!.Sent);

        // Third run is idempotent: no new reminders in the same period.
        var thirdReminderResponse = await _httpClient.PostAsync("/api/cron/checkins/reminders", null);
        thirdReminderResponse.EnsureSuccessStatusCode();
        var thirdReminderResult = await thirdReminderResponse.Content.ReadFromJsonAsync<DispatchResult>();
        Assert.Equal(0, thirdReminderResult!.Sent);
    }

    [Fact]
    public async Task Cron_endpoints_require_secret()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_app.BaseUrl) };

        var dispatchResponse = await client.PostAsync("/api/cron/checkins/dispatch", null);
        var remindersResponse = await client.PostAsync("/api/cron/checkins/reminders", null);

        Assert.Equal(HttpStatusCode.Unauthorized, dispatchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, remindersResponse.StatusCode);
    }

    [Fact]
    public async Task Check_in_and_invitation_emails_use_base_url_not_localhost_8080()
    {
        var (projectId, _, _) = await SeedActiveProjectAsync();

        // The invitation created during seeding already used the captured e-mail service.
        var invitationEmail = _app.CapturedEmails.GetAll().Single();
        Assert.Contains(_app.BaseUrl, invitationEmail.HtmlBody);
        Assert.DoesNotContain("localhost:8080", invitationEmail.HtmlBody);

        var dispatchResponse = await _httpClient.PostAsync("/api/cron/checkins/dispatch", null);
        dispatchResponse.EnsureSuccessStatusCode();

        var checkInEmail = _app.CapturedEmails.GetAll().Last();
        Assert.Contains($"{_app.BaseUrl}/projects/{projectId}/checkin", checkInEmail.HtmlBody);
        Assert.DoesNotContain("localhost:8080", checkInEmail.HtmlBody);
    }

    private async Task<(Guid ProjectId, Guid CreatorId, Guid PartnerId)> SeedActiveProjectAsync()
    {
        using var scope = _app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var userRegistration = services.GetRequiredService<IUserRegistrationService>();
        var projectService = services.GetRequiredService<IProjectService>();

        var creator = await userRegistration.RegisterAsync("Creator", "creator@test.com", "Password123!");
        var partner = await userRegistration.RegisterAsync("Partner", "partner@test.com", "Password123!");

        var start = DateTime.Today.ToUniversalTime();
        var end = start.AddMonths(1);
        var goals = new[]
        {
            new GoalFieldInput("Peso", GoalDataType.Decimal, "kg", 0, 300, 75)
        };

        var project = await projectService.CreateProjectAsync(
            creator.Id, "Projeto Demo", start, end, ProjectFrequency.Weekly, goals);

        var invitation = await projectService.InvitePartnerAsync(
            project.Id, creator.Id, "partner@test.com", _app.BaseUrl);

        await projectService.AcceptInvitationAsync(invitation.Token, partner.Id);
        await projectService.ApproveProjectAsync(project.Id, partner.Id);

        return (project.Id, creator.Id, partner.Id);
    }

    private async Task SubmitCheckInAsync(Guid projectId, Guid userId, decimal value)
    {
        using var scope = _app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var projectService = services.GetRequiredService<IProjectService>();
        var checkInService = services.GetRequiredService<ICheckInService>();

        var project = await projectService.GetProjectAsync(projectId, userId)
            ?? throw new InvalidOperationException("Project not found while seeding check-in.");

        var metric = new CheckInMetricInput(project.Goals.First().Id, value);
        await checkInService.SubmitCheckInAsync(projectId, userId, Feeling.Happy, new[] { metric });
    }

    private record DispatchResult(int Sent);
}
