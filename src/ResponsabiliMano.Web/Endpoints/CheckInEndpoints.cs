using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web.Models;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Check-in capture and edit endpoints under <c>/api/projects</c> (specs S3.2, S7.1).
/// The whole group is gated behind the <see cref="FeatureFlags.CheckIns"/> flag (spec R7):
/// while off, the route responds 404.
/// </summary>
public static class CheckInEndpoints
{
    public static IEndpointRouteBuilder MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireFeature(FeatureFlags.CheckIns);

        group.MapPost("/{id:guid}/checkins", SubmitAsync).DisableAntiforgery();
        group.MapPut("/{id:guid}/checkins/current", UpdateCurrentAsync).DisableAntiforgery();
        group.MapDelete("/{id:guid}/checkins/current", DeleteCurrentAsync).DisableAntiforgery();

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

    private static async Task<IResult> UpdateCurrentAsync(
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

        return await ExecuteEditDeleteAsync(async () =>
        {
            var checkIn = await checkInService.UpdateCurrentCheckInAsync(
                id, userId, request.Feeling, metrics, cancellationToken);

            if (checkIn is null)
                return Results.NotFound(new { error = "Project not found." });

            return Results.Ok(new { checkIn.Id, checkIn.PeriodNumber, checkIn.Feeling, checkIn.UpdatedAt });
        });
    }

    private static async Task<IResult> DeleteCurrentAsync(
        Guid id,
        HttpContext httpContext,
        ICheckInService checkInService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        return await ExecuteEditDeleteAsync(async () =>
        {
            var deleted = await checkInService.DeleteCurrentCheckInAsync(
                id, userId, cancellationToken);

            if (!deleted)
                return Results.NotFound(new { error = "Project not found." });

            return Results.NoContent();
        });
    }

    private static async Task<IResult> ExecuteEditDeleteAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
