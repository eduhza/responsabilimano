using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Dashboard data endpoint under <c>/api/projects</c> (spec S4.1). The whole
/// group is gated behind the <see cref="FeatureFlags.Dashboard"/> flag (spec R7):
/// while off, the route responds 404.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireFeature(FeatureFlags.Dashboard);

        group.MapGet("/{id:guid}/dashboard", GetDashboardAsync);

        return app;
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
