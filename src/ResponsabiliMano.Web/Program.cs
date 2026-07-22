using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.DependencyInjection;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Web.Components;
using ResponsabiliMano.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddResponsabiliManoInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();

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

    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
