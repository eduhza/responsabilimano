using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ResponsabiliMano.Web.E2ETests;

public static class PlaywrightExtensions
{
    /// <summary>
    /// Navigates to an interactive Blazor page and waits until the circuit is ready.
    /// </summary>
    public static async Task GotoAndWaitForBlazorAsync(this IPage page, string url)
    {
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Fills the auth login form and submits it.
    /// </summary>
    public static async Task LoginAsync(this IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAndWaitForBlazorAsync($"{baseUrl}/login");
        await page.WaitForSelectorAsync("h3:has-text('Entrar')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#email").FillAsync(email);
        await page.Locator("#password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();

        await page.WaitForURLAsync($"{baseUrl}/");
    }

    /// <summary>
    /// Fills the registration form and submits it.
    /// </summary>
    public static async Task RegisterAsync(this IPage page, string baseUrl, string name, string email, string password)
    {
        var logFile = Path.Combine(Path.GetTempPath(), $"e2e-playwright-{Guid.NewGuid():N}.log");
        void Log(string s) => File.AppendAllText(logFile, s + Environment.NewLine);
        page.Request += (_, request) => Log($"> {request.Method} {request.Url}");
        page.Response += (_, response) => Log($"< {response.Status} {response.Url}");
        page.Console += (_, msg) => Log($"[console] {msg.Type}: {msg.Text}");

        await page.GotoAsync($"{baseUrl}/register");
        await page.WaitForSelectorAsync("h3:has-text('Cadastro')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#name").FillAsync(name);
        await page.Locator("#email").FillAsync(email);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("#confirmPassword").FillAsync(password);

        await page.GetByRole(AriaRole.Button, new() { Name = "Cadastrar" }).ClickAsync();

        try
        {
            await page.WaitForURLAsync($"{baseUrl}/");
        }
        catch
        {
            var html = await page.ContentAsync();
            var dumpPath = Path.Combine(Path.GetTempPath(), $"register-timeout-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(dumpPath, html);
            Console.WriteLine($"==== PAGE HTML DUMPED TO {dumpPath} ====");
            throw;
        }
    }

    /// <summary>
    /// Creates a new project through the UI and returns the invite link href.
    /// </summary>
    public static async Task<string> CreateProjectAsync(this IPage page, string baseUrl, string name, string unit, decimal min, decimal max, decimal target)
    {
        await page.GotoAndWaitForBlazorAsync($"{baseUrl}/projects/new");
        await page.WaitForSelectorAsync("h3:has-text('Criar Projeto')");

        await page.Locator("#name").FillAsync(name);
        await page.GetByLabel("Data de Início").FillAsync(DateTime.Today.ToString("yyyy-MM-dd"));
        await page.GetByLabel("Data de Fim").FillAsync(DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"));

        // Goal fields have no id/for attributes, so select by order in the card.
        var goalFields = page.Locator(".card .form-control");
        await goalFields.Nth(0).FillAsync(name);        // label
        await goalFields.Nth(1).SelectOptionAsync("Decimal");
        await goalFields.Nth(2).FillAsync(unit);        // unit
        await goalFields.Nth(3).FillAsync(min.ToString());
        await goalFields.Nth(4).FillAsync(max.ToString());
        await goalFields.Nth(5).FillAsync(target.ToString());

        await page.GetByRole(AriaRole.Button, new() { Name = "Criar Projeto" }).ClickAsync();
        await page.WaitForSelectorAsync("div.alert-success");

        var link = await page.Locator("a[href$='/invite']").First.GetAttributeAsync("href");
        return link!;
    }

    /// <summary>
    /// Extracts the first link matching the given href regex from an HTML body.
    /// </summary>
    public static string ExtractFirstLink(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[0].Value : throw new InvalidOperationException("Link not found in e-mail body.");
    }

    /// <summary>
    /// Fails the test if the Blazor error UI is visible.
    /// </summary>
    public static async Task AssertNoBlazorErrorsAsync(this IPage page)
    {
        var errorUi = page.Locator("#blazor-error-ui");
        if (await errorUi.IsVisibleAsync())
        {
            var text = await errorUi.InnerTextAsync();
            Assert.Fail($"Blazor error UI visible: {text}");
        }
    }
}
