using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web.Models;
using System.Security.Claims;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Authentication and account-recovery endpoints under <c>/api/auth</c>.
/// Behaviour is preserved 1:1 from the original inline mapping (spec R1);
/// antiforgery decisions are revisited separately in spec R5.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync).DisableAntiforgery();
        // Cast to Delegate: this handler's only parameter is HttpContext, which
        // would otherwise bind as a RequestDelegate and discard the redirect (ASP0016).
        group.MapPost("/logout", (Delegate)LogoutAsync).DisableAntiforgery();
        group.MapPost("/forgot-password", ForgotPasswordAsync).DisableAntiforgery();
        group.MapPost("/reset-password", ResetPasswordAsync).DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, IUserRegistrationService service, CancellationToken cancellationToken)
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
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginRequest request, IUserLoginService loginService, HttpContext httpContext)
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
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request, IPasswordResetService resetService, CancellationToken cancellationToken)
    {
        if (!EmailAddress.IsValid(request.Email))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["email"] = ["A valid email is required."]
            });

        await resetService.RequestResetAsync(request.Email, cancellationToken);
        return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request, IPasswordResetService resetService, CancellationToken cancellationToken)
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
    }
}
