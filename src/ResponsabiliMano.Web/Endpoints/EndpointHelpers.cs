using System.Security.Claims;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Shared helpers for the minimal API endpoints: authenticated-user resolution and
/// a common mapping from domain exceptions to HTTP results.
/// </summary>
public static class EndpointHelpers
{
    /// <summary>
    /// Resolves the authenticated user's id from the current principal.
    /// Returns <c>false</c> when the request is unauthenticated or the id claim is missing/invalid.
    /// </summary>
    public static bool TryGetAuthenticatedUserId(this HttpContext httpContext, out Guid userId)
    {
        userId = Guid.Empty;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdStr is not null && Guid.TryParse(userIdStr, out userId);
    }

    /// <summary>
    /// Runs an endpoint body, mapping the domain exceptions raised by the services to their
    /// canonical HTTP results: <see cref="ArgumentException"/> -> 400, <see cref="UnauthorizedAccessException"/> -> 403,
    /// <see cref="InvalidOperationException"/> -> 409.
    /// </summary>
    public static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
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
