using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.DependencyInjection;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Web.Components;
using ResponsabiliMano.Web.Endpoints;
using Microsoft.FeatureManagement;
using Serilog;

var app = await Program.CreateAppAsync(new WebApplicationOptions { Args = args });
app.Run();

public partial class Program
{
    public static async Task<WebApplication> CreateAppAsync(
        WebApplicationOptions? options = null,
        Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(options ?? new WebApplicationOptions());

        // Structured logging (spec R8): configure Serilog from configuration.
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddFluentUIComponents();

        builder.Services.AddLocalization();
        builder.Services.AddResponsabiliManoInfrastructure(builder.Configuration);

        // Feature flags (spec R7): deploy != release. Flags live in the FeatureManagement config section.
        builder.Services.AddFeatureManagement();

        // Health checks (spec R8): liveness is dependency-free; readiness verifies the database.
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "ResponsabiliMano.Auth";
                options.LoginPath = "/login";
                options.LogoutPath = "/api/auth/logout";
                options.AccessDeniedPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        builder.Services.AddScoped<IUserLoginService, UserLoginService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();

        // Tests can override connection strings, e-mail services, etc.
        configure?.Invoke(builder);

        var app = builder.Build();

        // Apply migrations in every environment; seed demo data only in development.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                await db.Database.EnsureCreatedAsync();
            }
            else
            {
                await db.Database.MigrateAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                await SeedData.SeedAsync(db);
            }
        }

        // Configure the HTTP request pipeline.
        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        // Re-execute error responses to the Blazor not-found page — for the UI only. The
        // /api surface is excluded: re-executing an empty-body API error (e.g. 401 from
        // Unauthorized(), 404 from a disabled feature gate) as a POST to the /not-found
        // Razor component hits antiforgery and turns every such response into an empty 400.
        // APIs must keep their real status codes.
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRequestLocalization(new RequestLocalizationOptions()
            .SetDefaultCulture("pt-BR")
            .AddSupportedCultures("pt-BR", "en")
            .AddSupportedUICultures("pt-BR", "en"));

        // Antiforgery guards the Blazor UI (server-rendered forms). The /api surface is
        // exempt: those endpoints authenticate via the auth cookie (SameSite=Lax already
        // mitigates cross-site POST) or the cron secret, and are never called by the Blazor
        // UI (which invokes the domain services directly). Also exempt Blazor's SignalR
        // circuit and framework scripts so InteractiveServer can connect and run forms.
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api")
                && !context.Request.Path.StartsWithSegments("/_"),
            branch => branch.UseAntiforgery());

        // Health endpoints (spec R8): "/health" is liveness (no dependencies checked);
        // "/health/ready" is readiness (database reachable). Both are anonymous.
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

        // HTTP endpoints, grouped by area (spec R1). Program.cs only composes.
        app.MapAuthEndpoints();
        app.MapProjectEndpoints();
        // Sprint 3 (specs S3.2–S3.4): check-in capture + scheduler jobs. Both groups are
        // gated behind the CheckIns feature flag and ship dark until Gate 3.
        app.MapCheckInEndpoints();
        app.MapCronEndpoints();
        // Sprint 4 (spec S4.1): dashboard data API. Gated behind the Dashboard feature flag.
        app.MapDashboardEndpoints();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
