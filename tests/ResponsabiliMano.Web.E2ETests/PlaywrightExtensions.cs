using System.Globalization;
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
    /// Fills a Fluent UI input component (fluent-text-field or fluent-number-field) by setting its value property and dispatching events.
    /// </summary>
    public static async Task FillFluentInputAsync(this ILocator locator, string value)
    {
        await locator.EvaluateAsync<string>("""
            (el, value) => {
                var tag = el.tagName.toLowerCase();
                if (tag === 'fluent-number-field') {
                    el.min = -1000000000;
                    el.max = 1000000000;
                }
                el.value = value;
                if ('currentValue' in el) {
                    el.currentValue = value;
                }
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                return value;
            }
            """, value);
    }

    /// <summary>
    /// Selects a Fluent UI select option by value.
    /// </summary>
    public static async Task SelectFluentOptionAsync(this ILocator locator, string value)
    {
        await locator.EvaluateAsync<string>("""
            (el, value) => {
                el.value = value;
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
                return value;
            }
            """, value);
    }

    /// <summary>
    /// Creates a new project through the UI and returns the invite link href.
    /// </summary>
    public static async Task<string> CreateProjectAsync(this IPage page, string baseUrl, string name, string unit, decimal min, decimal max, decimal target)
    {
        await page.GotoAndWaitForBlazorAsync($"{baseUrl}/projects/new");
        await page.WaitForSelectorAsync("h3:has-text('Criar Projeto')");

        var ptBr = new CultureInfo("pt-BR");

        await page.Locator("fluent-text-field#name").FillFluentInputAsync(name);
        await page.GetByLabel("Data de Início").FillFluentInputAsync(DateTime.Today.ToString("d", ptBr));
        await page.GetByLabel("Data de Fim").FillFluentInputAsync(DateTime.Today.AddMonths(1).ToString("d", ptBr));
        await page.Locator("fluent-select#frequency").SelectFluentOptionAsync("Weekly");

        // Goal fields within the goal card.
        var goalTextFields = page.Locator(".goal-card fluent-text-field");
        var goalSelects = page.Locator(".goal-card fluent-select");
        var goalNumberFields = page.Locator(".goal-card fluent-number-field");

        await goalTextFields.Nth(0).FillFluentInputAsync(name);        // label
        await goalSelects.Nth(0).SelectFluentOptionAsync("Decimal");   // data type
        await goalTextFields.Nth(1).FillFluentInputAsync(unit);        // unit
        await goalNumberFields.Nth(0).FillFluentInputAsync(min.ToString(ptBr));   // min
        await goalNumberFields.Nth(1).FillFluentInputAsync(max.ToString(ptBr));   // max
        await goalNumberFields.Nth(2).FillFluentInputAsync(target.ToString(ptBr)); // target

        await page.GetByRole(AriaRole.Button, new() { Name = "Criar Projeto" }).ClickAsync();

        var inviteLink = page.GetByRole(AriaRole.Link, new() { Name = "Convidar Parceiro" });
        await inviteLink.WaitForAsync();
        await inviteLink.ClickAsync();

        var inviteUrlPattern = $"{Regex.Escape(baseUrl)}/projects/[0-9a-fA-F-]+/invite";
        await page.WaitForURLAsync(new Regex($"^{inviteUrlPattern}$"));

        return new Uri(page.Url).PathAndQuery;
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
