using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Dashboard data endpoints: per project (spec S4.1) and account-wide (spec S6.1).
/// Both hang off one <c>/api</c> group so the whole feature is gated by a single
/// <see cref="FeatureFlags.Dashboard"/> filter (spec R7): while off, the routes
/// respond 404.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireFeature(FeatureFlags.Dashboard);

        group.MapGet("/projects/{id:guid}/dashboard", GetDashboardAsync);
        group.MapGet("/dashboard", GetGlobalDashboardAsync);

        return app;
    }

    private static async Task<IResult> GetGlobalDashboardAsync(
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        return await EndpointHelpers.ExecuteAsync(async () =>
            Results.Ok(await dashboardService.GetGlobalDashboardAsync(userId, cancellationToken)));
    }

    private static async Task<IResult> GetDashboardAsync(
        Guid id,
        HttpContext httpContext,
        IDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        return await EndpointHelpers.ExecuteAsync(async () =>
        {
            var dashboard = await dashboardService.GetDashboardAsync(id, userId, cancellationToken);
            if (dashboard is null)
                return Results.NotFound();

            return Results.Ok(dashboard);
        });
    }
}
