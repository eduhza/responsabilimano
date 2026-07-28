using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Data.Sqlite;

namespace ResponsabiliMano.Web.E2ETests;

/// <summary>
/// Spins up the real web app with <c>dotnet run</c> on a random loopback port, backed by
/// a temporary SQLite database. In the Testing environment the app exposes test-only
/// endpoints for resetting state and listing captured e-mails, which the E2E suite uses
/// instead of direct DI access.
/// </summary>
public class ResponsabiliManoApp : IAsyncLifetime
{
    private Process? _process;
    private string? _dbPath;
    private HttpClient? _httpClient;

    public string BaseUrl { get; private set; } = null!;
    public HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException("App has not been initialized.");

    public async Task InitializeAsync()
    {
        var dbDirectory = Path.Combine(Path.GetTempPath(), "ResponsabiliMano-E2E");
        Directory.CreateDirectory(dbDirectory);
        _dbPath = Path.Combine(dbDirectory, $"e2e-{Guid.NewGuid():N}.db");

        var webProjectDir = FindWebProjectDirectory();
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-launch-profile --configuration Release --no-build -- --urls {BaseUrl}",
            WorkingDirectory = webProjectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["E2E"] = "true",
                ["ConnectionStrings__DefaultConnection"] = $"DataSource={_dbPath}",
                ["FeatureManagement__CheckIns"] = "true",
                ["Cron__Secret"] = "test-cron-secret",
                ["Logging__LogLevel__Default"] = "Warning",
                ["Logging__LogLevel__Microsoft.AspNetCore"] = "Warning"
            }
        };

        _process = new Process { StartInfo = startInfo };
        _process.OutputDataReceived += (_, e) => LogOutput("OUT", e.Data);
        _process.ErrorDataReceived += (_, e) => LogOutput("ERR", e.Data);
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitUntilReadyAsync();
    }

    public Task ResetDatabaseAsync() => HttpClient.PostAsync("/api/_test/reset", new StringContent(string.Empty));

    public async Task<IReadOnlyList<EmailMessage>> GetEmailsAsync()
    {
        var response = await HttpClient.GetAsync("/api/_test/emails");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<EmailMessage>>(
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            });
        return result ?? [];
    }

    public async Task<IReadOnlyList<EmailMessage>> GetEmailsForAsync(string to)
    {
        var all = await GetEmailsAsync();
        return all.Where(e => e.To.Equals(to, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<EmailMessage?> GetLastEmailForAsync(string to)
    {
        var emails = await GetEmailsForAsync(to);
        return emails.LastOrDefault();
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();

        if (_process is not null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            catch
            {
                // Best effort cleanup.
            }
            _process.Dispose();
        }

        SqliteConnection.ClearAllPools();
        if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static string FindWebProjectDirectory()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(typeof(ResponsabiliManoApp).Assembly.Location)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ResponsabiliMano.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not locate repository root.");
        }

        return Path.Combine(dir.FullName, "src", "ResponsabiliMano.Web");
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(2) };
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < TimeSpan.FromSeconds(60))
        {
            try
            {
                var response = await client.GetAsync("/health");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // The app is still starting.
            }

            await Task.Delay(250);
            if (_process?.HasExited == true)
            {
                throw new InvalidOperationException($"Web app process exited early with code {_process.ExitCode}.");
            }
        }

        throw new TimeoutException("Timed out waiting for the web app to be ready.");
    }

    private static void LogOutput(string prefix, string? data)
    {
        if (!string.IsNullOrWhiteSpace(data))
        {
            var log = Path.Combine(Path.GetTempPath(), "e2e-webapp.log");
            File.AppendAllText(log, $"[{prefix}] {data}{Environment.NewLine}");
        }
    }
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody);
