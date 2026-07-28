using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Web.E2ETests;

/// <summary>
/// In-process integration host for tests that do not need a browser. It reuses the
/// web entry point and overrides the database and e-mail services exactly like the
/// original fixture so that assertions can run against persisted state and captured
/// e-mails without changing production code paths.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ResponsabiliManoWebApp> { }

public class ResponsabiliManoWebApp : IAsyncLifetime
{
    private WebApplication? _app;
    private string? _dbPath;

    public string BaseUrl { get; private set; } = null!;
    public IServiceProvider Services => _app?.Services ?? throw new InvalidOperationException("App not started.");
    public CapturedEmailService CapturedEmails { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var dbDirectory = Path.Combine(Path.GetTempPath(), "ResponsabiliMano-E2E");
        Directory.CreateDirectory(dbDirectory);
        _dbPath = Path.Combine(dbDirectory, $"e2e-{Guid.NewGuid():N}.db");
        CapturedEmails = new CapturedEmailService();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"DataSource={_dbPath}");
        Environment.SetEnvironmentVariable("FeatureManagement__CheckIns", "true");
        Environment.SetEnvironmentVariable("Cron__Secret", "test-cron-secret");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var webAssemblyPath = typeof(Program).Assembly.Location;
        var webProjectDir = new DirectoryInfo(Path.GetDirectoryName(webAssemblyPath)!).Parent!.Parent!.Parent!.FullName;

        _app = await Program.CreateAppAsync(
            new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                ContentRootPath = webProjectDir,
                ApplicationName = "ResponsabiliMano.Web"
            },
            configure: builder =>
        {
            builder.Environment.EnvironmentName = "Testing";

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });

            builder.Services.RemoveAll<DbContextOptions>();
            builder.Services.RemoveAll<DbContextOptions<AppDbContext>>();
            builder.Services.RemoveAll<AppDbContext>();

            builder.Services.AddSingleton(sp =>
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"DataSource={_dbPath}")
                    .Options);

            builder.Services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));
            builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

            builder.Services.RemoveAll<IEmailService>();
            builder.Services.AddSingleton<IEmailService>(CapturedEmails);

            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(IPAddress.Loopback, 0));
        });

        await _app.StartAsync();

        var server = _app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses!.Addresses
            .Where(a => a.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? addresses.Addresses.Last();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM check_in_metrics;" +
            "DELETE FROM check_ins;" +
            "DELETE FROM check_in_notifications;" +
            "DELETE FROM project_change_requests;" +
            "DELETE FROM project_invitations;" +
            "DELETE FROM goal_fields;" +
            "DELETE FROM password_reset_tokens;" +
            "DELETE FROM projects;" +
            "DELETE FROM users;");

        CapturedEmails.Clear();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        SqliteConnection.ClearAllPools();
        if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
