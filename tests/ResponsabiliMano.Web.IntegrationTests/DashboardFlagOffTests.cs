using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Web.Services;

namespace ResponsabiliMano.Web.IntegrationTests;

public sealed class DashboardFlagOffFixture : IAsyncLifetime
{
    private WebApplication? _app;
    private string? _dbPath;

    public string BaseUrl { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var dbDirectory = Path.Combine(Path.GetTempPath(), "ResponsabiliMano-Integration");
        Directory.CreateDirectory(dbDirectory);
        _dbPath = Path.Combine(dbDirectory, $"int-{Guid.NewGuid():N}.db");

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"DataSource={_dbPath}");
        Environment.SetEnvironmentVariable("FeatureManagement__CheckIns", "true");
        Environment.SetEnvironmentVariable("FeatureManagement__Dashboard", "false");
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
                    ["FeatureManagement:CheckIns"] = "true",
                    ["FeatureManagement:Dashboard"] = "false",
                    ["Cron:Secret"] = "test-cron-secret"
                });

                builder.Services.RemoveAll<DbContextOptions>();
                builder.Services.RemoveAll<DbContextOptions<AppDbContext>>();
                builder.Services.RemoveAll<AppDbContext>();
                builder.Services.AddSingleton(sp =>
                    new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"DataSource={_dbPath}").Options);
                builder.Services.AddScoped<AppDbContext>(sp =>
                    new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));
                builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
                builder.Services.RemoveAll<IEmailService>();
                builder.Services.AddSingleton<IEmailService>(new CapturedEmailService());

                builder.Services.Configure<CookieAuthenticationOptions>(
                    CookieAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.Events = new CookieAuthenticationEvents
                        {
                            OnRedirectToLogin = ctx =>
                            {
                                if (ctx.Request.Path.StartsWithSegments("/api"))
                                {
                                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return Task.CompletedTask;
                                }
                                ctx.Response.Redirect(ctx.RedirectUri);
                                return Task.CompletedTask;
                            },
                            OnRedirectToAccessDenied = ctx =>
                            {
                                if (ctx.Request.Path.StartsWithSegments("/api"))
                                {
                                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                    return Task.CompletedTask;
                                }
                                ctx.Response.Redirect(ctx.RedirectUri);
                                return Task.CompletedTask;
                            }
                        };
                    });

                builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
            });

        await _app.StartAsync();
        var server = _app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses!.Addresses
            .Where(a => a.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? addresses.Addresses.Last();
        Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null) { await _app.StopAsync(); await _app.DisposeAsync(); }
        SqliteConnection.ClearAllPools();
        if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

[CollectionDefinition("DashboardFlagOff")]
public class DashboardFlagOffCollection : ICollectionFixture<DashboardFlagOffFixture> { }

[Collection("DashboardFlagOff")]
public class DashboardFlagOffTests : IAsyncLifetime
{
    private readonly DashboardFlagOffFixture _fixture;
    public DashboardFlagOffTests(DashboardFlagOffFixture fixture) => _fixture = fixture;
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboard_FeatureFlagOff_Returns404()
    {
        var response = await _fixture.Client.GetAsync($"/api/projects/{Guid.NewGuid()}/dashboard");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGlobalDashboard_FeatureFlagOff_Returns404()
    {
        var response = await _fixture.Client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
