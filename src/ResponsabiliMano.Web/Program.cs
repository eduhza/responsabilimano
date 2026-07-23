using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.DependencyInjection;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Web.Components;
using ResponsabiliMano.Web.Endpoints;
using ResponsabiliMano.Web.Models;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Enums;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddResponsabiliManoInfrastructure(builder.Configuration);
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

var app = builder.Build();

// Apply migrations and seed in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("pt-BR")
    .AddSupportedCultures("pt-BR")
    .AddSupportedUICultures("pt-BR"));

app.UseAntiforgery();

app.MapPost("/api/auth/register", async (RegisterRequest request, IUserRegistrationService service, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add("name", ["Name is required."]);

    if (!EmailAddress.IsValid(request.Email))
        errors.Add("email", ["A valid email is required."]);

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        errors.Add("password", ["Password must be at least 8 characters."]);

    if (request.Password != request.ConfirmPassword)
        errors.Add("confirmPassword", ["Passwords do not match."]);

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    try
    {
        var user = await service.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Name, user.Email });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/auth/login", async ([FromForm] LoginRequest request, IUserLoginService loginService, HttpContext httpContext) =>
{
    var user = await loginService.LoginAsync(request.Email, request.Password);

    if (user is null)
    {
        return Results.Redirect("/login?error=InvalidCredentials");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Name),
        new(ClaimTypes.Email, user.Email)
    };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
    };

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapPost("/api/auth/forgot-password", async (ForgotPasswordRequest request, IPasswordResetService resetService, CancellationToken cancellationToken) =>
{
    if (!EmailAddress.IsValid(request.Email))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["A valid email is required."]
        });

    await resetService.RequestResetAsync(request.Email, cancellationToken);
    return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
}).DisableAntiforgery();

app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest request, IPasswordResetService resetService, CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Token))
        errors.Add("token", ["Token is required."]);

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        errors.Add("password", ["Password must be at least 8 characters."]);

    if (request.Password != request.ConfirmPassword)
        errors.Add("confirmPassword", ["Passwords do not match."]);

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var success = await resetService.ResetPasswordAsync(request.Token, request.Password, cancellationToken);

    if (!success)
        return Results.BadRequest(new { error = "Invalid or expired token." });

    return Results.Ok(new { message = "Password reset successfully." });
}).DisableAntiforgery();

app.MapPost("/api/projects", async (HttpContext httpContext, CreateProjectRequest request, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add("name", ["Project name is required."]);

    if (request.EndDate <= request.StartDate)
        errors.Add("endDate", ["End date must be after start date."]);

    if (request.Goals.Count == 0)
        errors.Add("goals", ["At least one goal is required."]);

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        var goals = request.Goals.Select(g => new GoalFieldInput(
            g.Label, g.DataType, g.Unit, g.MinValue, g.MaxValue, g.TargetValue));

        var project = await projectService.CreateProjectAsync(
            userId, request.Name, request.StartDate, request.EndDate,
            request.Frequency, goals, cancellationToken);

        return Results.Created($"/api/projects/{project.Id}", new { project.Id, project.Name, project.Status });
    });
}).DisableAntiforgery();

app.MapPost("/api/projects/{id:guid}/invite", async (Guid id, HttpContext httpContext, InvitePartnerRequest request, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    if (!EmailAddress.IsValid(request.PartnerEmail))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["partnerEmail"] = ["A valid email is required."]
        });

    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        var invitation = await projectService.InvitePartnerAsync(id, userId, request.PartnerEmail, baseUrl, cancellationToken);
        return Results.Ok(new { invitation.Id, invitation.Email, invitation.ExpiresAt });
    });
}).DisableAntiforgery();

app.MapGet("/api/projects/{id:guid}", async (Guid id, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        var project = await projectService.GetProjectAsync(id, userId, cancellationToken);
        if (project is null)
            return Results.NotFound();

        return Results.Ok(new
        {
            project.Id,
            project.Name,
            project.StartDate,
            project.EndDate,
            project.Frequency,
            project.Status,
            CreatorName = project.Creator.Name,
            PartnerName = project.Partner?.Name,
            Goals = project.Goals.Select(g => new { g.Id, g.Label, g.DataType, g.Unit, g.MinValue, g.MaxValue, g.TargetValue }),
            ChangeRequests = project.ChangeRequests.Select(cr => new
            {
                cr.Id, cr.Type, cr.Status, cr.CreatedAt, cr.RequestedByUserId
            })
        });
    });
});

app.MapPost("/api/projects/{id:guid}/approve", async (Guid id, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        await projectService.ApproveProjectAsync(id, userId, cancellationToken);
        return Results.Ok(new { message = "Project approved." });
    });
}).DisableAntiforgery();

app.MapPost("/api/projects/{id:guid}/change-requests", async (Guid id, HttpContext httpContext, ProposeChangeRequest request, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        var payloadJson = request.ToPayloadJson();
        var changeRequest = await projectService.ProposeChangeAsync(id, userId, request.Type, payloadJson, cancellationToken);
        return Results.Created($"/api/projects/{id}/change-requests/{changeRequest.Id}", new { changeRequest.Id, changeRequest.Status });
    });
}).DisableAntiforgery();

app.MapPost("/api/projects/{id:guid}/change-requests/{crId:guid}/respond", async (Guid id, Guid crId, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken) =>
{
    if (!httpContext.TryGetAuthenticatedUserId(out var userId))
        return Results.Unauthorized();

    var approveStr = httpContext.Request.Query["approve"];
    if (!bool.TryParse(approveStr, out var approve))
        return Results.BadRequest(new { error = "The 'approve' query parameter must be true or false." });

    return await EndpointHelpers.ExecuteAsync(async () =>
    {
        await projectService.RespondToChangeRequestAsync(id, crId, userId, approve, cancellationToken);
        return Results.Ok(new { message = approve ? "Change request approved." : "Change request rejected." });
    });
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
