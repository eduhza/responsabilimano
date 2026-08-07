using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web.Models;

namespace ResponsabiliMano.Web.Endpoints;

/// <summary>
/// Project lifecycle endpoints under <c>/api/projects</c>: creation, partner
/// invitation, retrieval, approval and change-request handling. Behaviour is
/// preserved 1:1 from the original inline mapping (spec R1); antiforgery
/// decisions are revisited separately in spec R5.
/// </summary>
public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects");

        group.MapPost("", CreateAsync).DisableAntiforgery();
        group.MapPost("/{id:guid}/invite", InviteAsync).DisableAntiforgery();
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/approve", ApproveAsync).DisableAntiforgery();
        group.MapPost("/{id:guid}/change-requests", ProposeChangeAsync).DisableAntiforgery();
        group.MapPost("/{id:guid}/change-requests/{crId:guid}/respond", RespondToChangeAsync).DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, CreateProjectRequest request, IProjectService projectService, CancellationToken cancellationToken)
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
                g.Goal.Label,
                g.Goal.DataType,
                g.Goal.Unit,
                g.Goal.MinValue,
                g.Goal.MaxValue,
                new GoalTargetInput(
                    g.CreatorTarget.Baseline,
                    g.CreatorTarget.TargetValue,
                    g.CreatorTarget.Direction),
                g.SuggestedPartnerTarget is null
                    ? null
                    : new GoalTargetInput(
                        g.SuggestedPartnerTarget.Baseline,
                        g.SuggestedPartnerTarget.TargetValue,
                        g.SuggestedPartnerTarget.Direction)));

            var project = await projectService.CreateProjectAsync(
                userId, request.Name, request.StartDate, request.EndDate,
                request.Frequency, goals, cancellationToken: cancellationToken);

            return Results.Created($"/api/projects/{project.Id}", new { project.Id, project.Name, project.Status });
        });
    }

    private static async Task<IResult> InviteAsync(
        Guid id, HttpContext httpContext, InvitePartnerRequest request, IProjectService projectService, CancellationToken cancellationToken)
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
    }

    private static async Task<IResult> GetAsync(
        Guid id, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken)
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
                Goals = project.Goals.Select(g => new
                {
                    g.Id,
                    g.Label,
                    g.DataType,
                    g.Unit,
                    g.MinValue,
                    g.MaxValue,
                    Targets = g.Targets.Select(t => new
                    {
                        t.UserId,
                        t.Baseline,
                        t.TargetValue,
                        t.Direction
                    })
                }),
                ChangeRequests = project.ChangeRequests.Select(cr => new
                {
                    cr.Id, cr.Type, cr.Status, cr.CreatedAt, cr.RequestedByUserId, cr.PayloadJson
                })
            });
        });
    }

    private static async Task<IResult> ApproveAsync(
        Guid id, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        return await EndpointHelpers.ExecuteAsync(async () =>
        {
            await projectService.ApproveProjectAsync(id, userId, cancellationToken);
            return Results.Ok(new { message = "Project approved." });
        });
    }

    private static async Task<IResult> ProposeChangeAsync(
        Guid id, HttpContext httpContext, ProposeChangeRequest request, IProjectService projectService, CancellationToken cancellationToken)
    {
        if (!httpContext.TryGetAuthenticatedUserId(out var userId))
            return Results.Unauthorized();

        return await EndpointHelpers.ExecuteAsync(async () =>
        {
            var payloadJson = request.ToPayloadJson();
            var changeRequest = await projectService.ProposeChangeAsync(id, userId, request.Type, payloadJson, cancellationToken);
            return Results.Created($"/api/projects/{id}/change-requests/{changeRequest.Id}", new { changeRequest.Id, changeRequest.Status });
        });
    }

    private static async Task<IResult> RespondToChangeAsync(
        Guid id, Guid crId, HttpContext httpContext, IProjectService projectService, CancellationToken cancellationToken)
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
    }
}
