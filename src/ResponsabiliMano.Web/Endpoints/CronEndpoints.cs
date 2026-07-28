using System.Security.Cryptography;
using System.Text;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Scheduler-driven check-in jobs under <c>/api/cron</c> (specs S3.3, S3.4). These
/// are machine-to-machine endpoints called by Cloud Scheduler; they are not public.
/// Authentication is a shared secret in the <c>X-Cron-Secret</c> header (see ADR-0005),
/// antiforgery is disabled (no cookie/session), and the whole group is behind the
/// <see cref="FeatureFlags.CheckIns"/> flag (spec R7).
/// </summary>
public static class CronEndpoints
{
    private const string SecretHeader = "X-Cron-Secret";

    public static IEndpointRouteBuilder MapCronEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cron").RequireFeature(FeatureFlags.CheckIns);

        group.MapPost("/checkins/dispatch", DispatchAsync).DisableAntiforgery();
        group.MapPost("/checkins/reminders", RemindersAsync).DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> DispatchAsync(
        HttpContext httpContext,
        ICheckInNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(httpContext))
            return Results.Unauthorized();

        var baseUrl = BaseUrl(httpContext);
        var sent = await notificationService.DispatchCheckInEmailsAsync(DateTime.UtcNow, baseUrl, cancellationToken);
        return Results.Ok(new { sent });
    }

    private static async Task<IResult> RemindersAsync(
        HttpContext httpContext,
        ICheckInNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(httpContext))
            return Results.Unauthorized();

        var baseUrl = BaseUrl(httpContext);
        var sent = await notificationService.DispatchRemindersAsync(DateTime.UtcNow, baseUrl, cancellationToken);
        return Results.Ok(new { sent });
    }

    private static bool IsAuthorized(HttpContext httpContext)
    {
        var expected = httpContext.RequestServices.GetRequiredService<IConfiguration>()["Cron:Secret"];
        if (string.IsNullOrEmpty(expected))
            return false; // fail closed: no secret configured means the jobs are unreachable

        var provided = httpContext.Request.Headers[SecretHeader].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }

    private static string BaseUrl(HttpContext httpContext)
        => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
}
