using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web.Models;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Check-in capture endpoint under <c>/api/projects</c> (spec S3.2). The whole
/// group is gated behind the <see cref="FeatureFlags.CheckIns"/> flag (spec R7):
/// while off, the route responds 404.
/// </summary>
public static class CheckInEndpoints
{
    public static IEndpointRouteBuilder MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireFeature(FeatureFlags.CheckIns);

        group.MapPost("/{id:guid}/checkins", SubmitAsync).DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        HttpContext httpContext,
        SubmitCheckInRequest request,
        ICheckInService checkInService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        var metrics = request.Metrics
            .Select(m => new CheckInMetricInput(m.GoalFieldId, m.Value))
            .ToList();

        return await EndpointHelpers.ExecuteAsync(async () =>
        {
            var checkIn = await checkInService.SubmitCheckInAsync(
                id, userId, request.Feeling, metrics, cancellationToken);

            return Results.Created(
                $"/api/projects/{id}/checkins/{checkIn.Id}",
                new { checkIn.Id, checkIn.PeriodNumber, checkIn.Feeling });
        });
    }
}
