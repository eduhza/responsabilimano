using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json;

namespace ResponsabiliMano.Web.IntegrationTests;

[Collection("Integration")]
public class ProjectEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;

    public ProjectEndpointsTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- GET /api/projects/{id} ----------

    [Fact]
    public async Task GetProject_Success_ReturnsOk()
    {
        var (creator, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(project.Name, body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetProject_NonParticipant_Returns403()
    {
        var (creator, _, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        // Register an outsider
        var outsiderCookie = await _fixture.RegisterAndLoginAsync("Outsider", "outsider@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(outsiderCookie);

        var response = await client.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_NonExistent_Returns404()
    {
        var cookie = await _fixture.RegisterAndLoginAsync("User", "user@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- POST /api/projects ----------

    [Fact]
    public async Task CreateProject_Success_ReturnsCreated()
    {
        var cookie = await _fixture.RegisterAndLoginAsync("Creator", "creator@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var request = new
        {
            Name = "New Project",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Frequency = 1, // Weekly
            Goals = new[] { new { Label = "Steps", DataType = 1, Unit = "count", TargetValue = 10000 } }
        };

        var response = await client.PostAsJsonAsync("/api/projects", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Project", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateProject_Invalid_ReturnsValidationProblem()
    {
        var cookie = await _fixture.RegisterAndLoginAsync("Creator", "creator2@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var request = new
        {
            Name = "",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(1), // end before start
            Frequency = 1,
            Goals = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync("/api/projects", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_Unauthenticated_Returns401()
    {
        var request = new
        {
            Name = "Test",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31),
            Frequency = 1,
            Goals = new[] { new { Label = "Steps", DataType = 1, Unit = "count" } }
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/projects", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- POST /api/projects/{id}/invite ----------

    [Fact]
    public async Task InvitePartner_Success_ReturnsOk()
    {
        var (creator, _, project) = await SeedHelper.SeedPendingProjectAsync(_fixture);
        // Register the partner so the email exists in the system
        await _fixture.RegisterAndLoginAsync("Partner", "partner@example.com", "Password123!");
        await _fixture.ResetAsync();
        // Re-seed since reset cleared DB
        var (creator2, _, project2) = await SeedHelper.SeedPendingProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator2.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project2.Id}/invite",
            new { PartnerEmail = "partner@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvitePartner_NonCreator_Returns403()
    {
        var (creator, partner, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        // Login as partner (not creator)
        var cookie = await _fixture.LoginAsync(partner.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/invite",
            new { PartnerEmail = "someone@example.com" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- POST /api/projects/{id}/approve ----------

    [Fact]
    public async Task ApproveProject_Success_ReturnsOk()
    {
        var (creator, partner, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        // Project is already Active (seeded as Active). Let's seed a pending project with partner.
        // Actually, let's use the partner to approve.
        var cookie = await _fixture.LoginAsync(partner.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.PostAsync($"/api/projects/{project.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveProject_NoPartner_Returns409()
    {
        var (creator, project) = await SeedHelper.SeedPendingProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.PostAsync($"/api/projects/{project.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- POST /api/projects/{id}/change-requests ----------

    [Fact]
    public async Task ProposeChange_Success_ReturnsCreated()
    {
        var (creator, partner, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var request = new
        {
            Type = 1, // Frequency
            NewFrequency = 2 // Monthly
        };

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/change-requests", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ProposeChange_InvalidPayload_Returns400()
    {
        var (creator, partner, project) = await SeedHelper.SeedActiveProjectAsync(_fixture);
        var cookie = await _fixture.LoginAsync(creator.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        // Type = EndDate (0) but no NewEndDate provided -> ToPayloadJson throws ArgumentException
        var request = new
        {
            Type = 0, // EndDate
            // NewEndDate missing
        };

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/change-requests", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- POST /api/projects/{id}/change-requests/{crId}/respond ----------

    [Fact]
    public async Task RespondToChangeRequest_Success_ReturnsOk()
    {
        var (cr, project, creator, partner) = await SeedHelper.SeedChangeRequestAsync(_fixture);
        // Partner responds to creator's change request
        var cookie = await _fixture.LoginAsync(partner.Email, "Password123!");
        var client = _fixture.AuthenticatedClient(cookie);

        var response = await client.PostAsync(
            $"/api/projects/{project.Id}/change-requests/{cr.Id}/respond?approve=true", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RespondToChangeRequest_NonParticipant_Returns403()
    {
        var (cr, project, creator, partner) = await SeedHelper.SeedChangeRequestAsync(_fixture);
        // Register an outsider
        var outsiderCookie = await _fixture.RegisterAndLoginAsync("Outsider", "outsider@example.com", "Password123!");
        var client = _fixture.AuthenticatedClient(outsiderCookie);

        var response = await client.PostAsync(
            $"/api/projects/{project.Id}/change-requests/{cr.Id}/respond?approve=true", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
