using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ResponsabiliMano.Web.E2ETests;

[Collection("E2E")]
public class ProjectAndCheckInFlowTests : IAsyncLifetime
{
    private readonly ResponsabiliManoApp _app;
    private readonly PlaywrightFixture _playwright;

    public ProjectAndCheckInFlowTests(ResponsabiliManoApp app, PlaywrightFixture playwright)
    {
        _app = app;
        _playwright = playwright;
    }

    public Task InitializeAsync() => _app.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_project_invite_accept_approve_and_check_in()
    {
        // 1. Register creator and partner in separate browser contexts.
        await using var creatorContext = await _playwright.Browser.NewContextAsync();
        var creatorPage = await creatorContext.NewPageAsync();
        await creatorPage.RegisterAsync(_app.BaseUrl, "Creator", "creator@test.com", "Password123!");

        await using var partnerContext = await _playwright.Browser.NewContextAsync();
        var partnerPage = await partnerContext.NewPageAsync();
        await partnerPage.RegisterAsync(_app.BaseUrl, "Partner", "partner@test.com", "Password123!");

        // 2. Creator creates a project.
        var inviteHref = await creatorPage.CreateProjectAsync(_app.BaseUrl, "Peso", "kg", 0, 300, 75);
        var projectId = ExtractProjectId(inviteHref);

        // 3. Creator invites partner.
        await creatorPage.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}{inviteHref}");
        await creatorPage.WaitForSelectorAsync("h3:has-text('Convidar Parceiro')");
        await creatorPage.Locator("#partnerEmail").FillFluentInputAsync("partner@test.com");
        await creatorPage.GetByRole(AriaRole.Button, new() { Name = "Convidar Parceiro" }).ClickAsync();
        await creatorPage.WaitForSelectorAsync("div.fluent-messagebar:has-text('Convite enviado')");

        var inviteEmail = await _app.GetLastEmailForAsync("partner@test.com");
        Assert.NotNull(inviteEmail);
        var invitationLink = PlaywrightExtensions.ExtractFirstLink(
            inviteEmail.HtmlBody,
            $"{Regex.Escape(_app.BaseUrl)}/invitations/[^\"'\\s]+");

        // 4. Partner accepts invitation and approves the project.
        await partnerPage.GotoAndWaitForBlazorAsync(invitationLink);
        await partnerPage.WaitForSelectorAsync("h4:has-text('Convite')");
        await partnerPage.GetByRole(AriaRole.Button, new() { Name = "Aceitar Convite" }).ClickAsync();

        var aprovarButton = partnerPage.GetByRole(AriaRole.Button, new() { Name = "Aprovar" });
        await aprovarButton.WaitForAsync();
        await aprovarButton.ClickAsync();

        var dialogAprovar = partnerPage.Locator("fluent-dialog").GetByRole(AriaRole.Button, new() { Name = "Aprovar" });
        await dialogAprovar.WaitForAsync();
        await dialogAprovar.ClickAsync();

        await partnerPage.WaitForSelectorAsync("div.fluent-messagebar:has-text('aprovado')");

        // 5. Partner fills the check-in.
        await partnerPage.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}/projects/{projectId}/checkin");
        await partnerPage.WaitForSelectorAsync("h3:has-text('Check-in')");

        var metricInputs = partnerPage.Locator("fluent-number-field");
        await metricInputs.First.FillFluentInputAsync("72");

        await partnerPage.Locator("button[title='Bem']").ClickAsync();
        await partnerPage.GetByRole(AriaRole.Button, new() { Name = "Enviar check-in" }).ClickAsync();
        await partnerPage.WaitForSelectorAsync("div.fluent-messagebar:has-text('Check-in registrado')");

        await partnerPage.AssertNoBlazorErrorsAsync();
    }

    [Fact]
    public async Task CheckIn_rejects_out_of_range_value()
    {
        var (projectId, _) = await SeedActiveProjectAsync();

        await using var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.LoginAsync(_app.BaseUrl, "creator@test.com", "Password123!");

        await page.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}/projects/{projectId}/checkin");
        await page.WaitForSelectorAsync("h3:has-text('Check-in')");

        await page.Locator("fluent-number-field").First.FillFluentInputAsync("9999");
        await page.Locator("button[title='Bem']").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar check-in" }).ClickAsync();

        var error = page.Locator("div.fluent-messagebar");
        var text = await error.TextContentAsync();
        Assert.Contains("maximum", text, StringComparison.OrdinalIgnoreCase);

        await page.AssertNoBlazorErrorsAsync();
    }

    [Fact]
    public async Task CheckIn_prevents_duplicate_submission()
    {
        var (projectId, _) = await SeedActiveProjectAsync();

        await using var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.LoginAsync(_app.BaseUrl, "creator@test.com", "Password123!");

        await page.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}/projects/{projectId}/checkin");
        await page.WaitForSelectorAsync("h3:has-text('Check-in')");

        await page.Locator("fluent-number-field").First.FillFluentInputAsync("70");
        await page.Locator("button[title='Bem']").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar check-in" }).ClickAsync();
        await page.WaitForSelectorAsync("div.fluent-messagebar:has-text('Check-in registrado')");

        await page.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}/projects/{projectId}/checkin");
        var alert = page.Locator("div.fluent-messagebar");
        var text = await alert.TextContentAsync();
        Assert.Contains("já registrou", text, StringComparison.OrdinalIgnoreCase);

        await page.AssertNoBlazorErrorsAsync();
    }

    private static string ExtractProjectId(string inviteHref)
    {
        var match = Regex.Match(inviteHref, "/projects/([0-9a-fA-F-]+)/invite");
        return match.Success ? match.Groups[1].Value : throw new InvalidOperationException("Could not extract project id.");
    }

    private async Task<(string ProjectId, string PartnerEmail)> SeedActiveProjectAsync()
    {
        // Seed through the same UI flow but return the project id for focused tests.
        await using var creatorContext = await _playwright.Browser.NewContextAsync();
        var creatorPage = await creatorContext.NewPageAsync();
        await creatorPage.RegisterAsync(_app.BaseUrl, "Creator", "creator@test.com", "Password123!");

        await using var partnerContext = await _playwright.Browser.NewContextAsync();
        var partnerPage = await partnerContext.NewPageAsync();
        await partnerPage.RegisterAsync(_app.BaseUrl, "Partner", "partner@test.com", "Password123!");

        var inviteHref = await creatorPage.CreateProjectAsync(_app.BaseUrl, "Peso", "kg", 0, 300, 75);
        var projectId = ExtractProjectId(inviteHref);

        await creatorPage.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}{inviteHref}");
        await creatorPage.Locator("#partnerEmail").FillFluentInputAsync("partner@test.com");
        await creatorPage.GetByRole(AriaRole.Button, new() { Name = "Convidar Parceiro" }).ClickAsync();
        await creatorPage.WaitForSelectorAsync("div.fluent-messagebar");

        var inviteEmail = await _app.GetLastEmailForAsync("partner@test.com");
        Assert.NotNull(inviteEmail);
        var invitationLink = PlaywrightExtensions.ExtractFirstLink(
            inviteEmail.HtmlBody,
            $"{Regex.Escape(_app.BaseUrl)}/invitations/[^\"'\\s]+");

        await partnerPage.GotoAndWaitForBlazorAsync(invitationLink);
        await partnerPage.GetByRole(AriaRole.Button, new() { Name = "Aceitar Convite" }).ClickAsync();

        var aprovarButton = partnerPage.GetByRole(AriaRole.Button, new() { Name = "Aprovar" });
        await aprovarButton.WaitForAsync();
        await aprovarButton.ClickAsync();

        var dialogAprovar = partnerPage.Locator("fluent-dialog").GetByRole(AriaRole.Button, new() { Name = "Aprovar" });
        await dialogAprovar.WaitForAsync();
        await dialogAprovar.ClickAsync();

        await partnerPage.WaitForSelectorAsync("div.fluent-messagebar:has-text('aprovado')");

        return (projectId, "partner@test.com");
    }
}
