using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Web.IntegrationTests;

[Collection("Integration")]
public class CheckInDashboardCronEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    public CheckInDashboardCronEndpointsTests(IntegrationFixture fixture) => _fixture = fixture;
    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SubmitCheckIn_Success_ReturnsCreated()
    {
        var (creator, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var goal = await db.GoalFields.FirstAsync(g => g.ProjectId == project.Id);
        var request = new { Feeling = 4, Metrics = new[] { new { GoalFieldId = goal.Id, Value = 5000m } } };
        var response = await client.PostAsJsonAsync($"/api/projects/{project.Id}/checkins", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SubmitCheckIn_Duplicate_Returns409()
    {
        var (creator, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var goal = await db.GoalFields.FirstAsync(g => g.ProjectId == project.Id);
        var request = new { Feeling = 4, Metrics = new[] { new { GoalFieldId = goal.Id, Value = 5000m } } };
        var resp1 = await client.PostAsJsonAsync($"/api/projects/{project.Id}/checkins", request);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        var resp2 = await client.PostAsJsonAsync($"/api/projects/{project.Id}/checkins", request);
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task SubmitCheckIn_NonParticipant_Returns403()
    {
        var (_, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var outsiderCookie = await _fixture.RegisterAndLoginAsync("Outsider", "outsider@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(outsiderCookie);
        var request = new { Feeling = 4, Metrics = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync($"/api/projects/{project.Id}/checkins", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubmitCheckIn_NonActiveProject_Returns409()
    {
        var (creator, project) = await SeedHelper.SeedPendingProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);
        var request = new { Feeling = 4, Metrics = Array.Empty<object>() };
        var response = await client.PostAsJsonAsync($"/api/projects/{project.Id}/checkins", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_Success_ReturnsOk()
    {
        var (creator, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);
        var response = await client.GetAsync($"/api/projects/{project.Id}/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_NonParticipant_Returns403()
    {
        var (_, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var outsiderCookie = await _fixture.RegisterAndLoginAsync("Outsider", "outsider@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(outsiderCookie);
        var response = await client.GetAsync($"/api/projects/{project.Id}/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CronDispatch_WithoutSecret_Returns401()
    {
        var response = await _fixture.Client.PostAsync("/api/cron/checkins/dispatch", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CronDispatch_WithSecret_ReturnsOk()
    {
        _fixture.Client.DefaultRequestHeaders.Add("X-Cron-Secret", "test-cron-secret");
        var response = await _fixture.Client.PostAsync("/api/cron/checkins/dispatch", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("sent", out _));
        _fixture.Client.DefaultRequestHeaders.Remove("X-Cron-Secret");
    }

    [Fact]
    public async Task CronReminders_WithoutSecret_Returns401()
    {
        var response = await _fixture.Client.PostAsync("/api/cron/checkins/reminders", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CronReminders_WithSecret_ReturnsOk()
    {
        _fixture.Client.DefaultRequestHeaders.Add("X-Cron-Secret", "test-cron-secret");
        var response = await _fixture.Client.PostAsync("/api/cron/checkins/reminders", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("sent", out _));
        _fixture.Client.DefaultRequestHeaders.Remove("X-Cron-Secret");
    }
}