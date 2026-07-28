using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ResponsabiliMano.Web.E2ETests;

[Collection("E2E")]
public class AuthFlowTests : IAsyncLifetime
{
    private readonly ResponsabiliManoApp _app;
    private readonly PlaywrightFixture _playwright;

    public AuthFlowTests(ResponsabiliManoApp app, PlaywrightFixture playwright)
    {
        _app = app;
        _playwright = playwright;
    }

    public Task InitializeAsync() => _app.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_and_login_flow_works()
    {
        await using var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.RegisterAsync(_app.BaseUrl, "Alice Test", "alice@test.com", "Password123!");
        await page.WaitForSelectorAsync("h1:has-text('Olá')");
        Assert.Contains("Alice Test", await page.ContentAsync());

        // Logout
        var logoutButton = page.GetByRole(AriaRole.Button, new() { Name = "Sair" });
        if (await logoutButton.IsVisibleAsync())
            await logoutButton.ClickAsync();

        await page.WaitForURLAsync($"{_app.BaseUrl}/login");

        // Login
        await page.LoginAsync(_app.BaseUrl, "alice@test.com", "Password123!");
        await page.WaitForSelectorAsync("h1:has-text('Alice Test')");

        await page.AssertNoBlazorErrorsAsync();
    }

    [Fact]
    public async Task Login_with_invalid_credentials_shows_error()
    {
        await using var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.RegisterAsync(_app.BaseUrl, "Bob Test", "bob@test.com", "Password123!");

        // Logout so we can test the login form with wrong credentials.
        var logoutButton = page.GetByRole(AriaRole.Button, new() { Name = "Sair" });
        if (await logoutButton.IsVisibleAsync())
            await logoutButton.ClickAsync();

        await page.WaitForURLAsync($"{_app.BaseUrl}/login");

        await page.GotoAsync($"{_app.BaseUrl}/login");
        await page.Locator("#email").FillAsync("bob@test.com");
        await page.Locator("#password").FillAsync("WrongPassword");
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();

        await page.WaitForURLAsync(url => url.StartsWith($"{_app.BaseUrl}/login?error=InvalidCredentials"));
        var error = page.Locator("div.alert-danger");
        Assert.Contains("inválidos", await error.TextContentAsync());

        await page.AssertNoBlazorErrorsAsync();
    }

    [Fact]
    public async Task Forgot_password_email_uses_base_url_and_reset_works()
    {
        await using var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.RegisterAsync(_app.BaseUrl, "Carol Test", "carol@test.com", "Password123!");

        // Logout before requesting password reset.
        var logoutButton = page.GetByRole(AriaRole.Button, new() { Name = "Sair" });
        if (await logoutButton.IsVisibleAsync())
            await logoutButton.ClickAsync();

        await page.WaitForURLAsync($"{_app.BaseUrl}/login");

        await page.GotoAndWaitForBlazorAsync($"{_app.BaseUrl}/forgot-password");
        await page.WaitForSelectorAsync("h3:has-text('Recuperar Senha')");

        await page.Locator("#email").FillAsync("carol@test.com");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar link de recuperação" }).ClickAsync();

        await page.WaitForSelectorAsync("div.alert-success:has-text('Se o e-mail existir')");

        var email = await _app.GetLastEmailForAsync("carol@test.com");
        Assert.NotNull(email);
        Assert.Contains(_app.BaseUrl, email.HtmlBody);
        Assert.DoesNotContain("localhost:8080", email.HtmlBody);

        var resetLink = PlaywrightExtensions.ExtractFirstLink(email.HtmlBody, $"{Regex.Escape(_app.BaseUrl)}/reset-password\\?token=[^\"'\\s]+");

        var resetContext = await _playwright.Browser.NewContextAsync();
        var resetPage = await resetContext.NewPageAsync();
        await resetPage.GotoAndWaitForBlazorAsync(resetLink);
        await resetPage.WaitForSelectorAsync("h3:has-text('Nova Senha')");

        await resetPage.Locator("#password").FillAsync("NewPassword123!");
        await resetPage.Locator("#confirmPassword").FillAsync("NewPassword123!");
        await resetPage.GetByRole(AriaRole.Button, new() { Name = "Redefinir senha" }).ClickAsync();

        await resetPage.WaitForSelectorAsync("div.alert-success:has-text('Senha redefinida')");

        // Login with new password
        await resetPage.LoginAsync(_app.BaseUrl, "carol@test.com", "NewPassword123!");
        await resetPage.WaitForSelectorAsync("h1:has-text('Carol Test')");

        await page.AssertNoBlazorErrorsAsync();
    }
}
