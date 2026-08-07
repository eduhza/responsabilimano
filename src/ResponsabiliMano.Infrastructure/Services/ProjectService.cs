using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class ProjectService : IProjectService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProjectService> _logger;

    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public ProjectService(AppDbContext context, IEmailService emailService, ILogger<ProjectService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Project> CreateProjectAsync(
        Guid creatorId,
        string name,
        DateTime startDate,
        DateTime endDate,
        ProjectFrequency frequency,
        IEnumerable<GoalFieldInput> goals,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(),
            CreatorId = creatorId,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Frequency = frequency,
            Status = ProjectStatus.Pending
        };

        foreach (var goal in goals)
        {
            if (string.IsNullOrWhiteSpace(goal.Label))
                throw new ArgumentException("Goal label is required.");

            if (string.IsNullOrWhiteSpace(goal.Unit))
                throw new ArgumentException("Goal unit is required.");

            EnsureValidDefinition(goal.Label.Trim(), goal.DataType, goal.MinValue, goal.MaxValue, goal.TargetValue);

            project.Goals.Add(new GoalField
            {
                Id = Guid.NewGuid(),
                Label = goal.Label.Trim(),
                DataType = goal.DataType,
                Unit = goal.Unit.Trim(),
                MinValue = Normalize(goal.DataType, goal.MinValue),
                MaxValue = Normalize(goal.DataType, goal.MaxValue),
                TargetValue = Normalize(goal.DataType, goal.TargetValue)
            });
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<ProjectInvitation> InvitePartnerAsync(
        Guid projectId,
        Guid inviterUserId,
        string partnerEmail,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != inviterUserId)
            throw new UnauthorizedAccessException("Only the project creator can invite partners.");

        var normalizedEmail = EmailAddress.Normalize(partnerEmail);

        if (project.CreatorId == await _context.Users
            .Where(u => u.Email.ToLower() == normalizedEmail)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken))
        {
            throw new ArgumentException("Cannot invite yourself.");
        }

        var token = SecureTokenGenerator.Generate();
        var invitation = new ProjectInvitation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Email = normalizedEmail,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(InvitationLifetime),
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        var inviteLink = $"{baseUrl.TrimEnd('/')}/invitations/{token}";
        var subject = EmailTemplates.ProjectInviteSubject;
        var body = EmailTemplates.ProjectInviteBody(project.Name, inviteLink);

        // The recipient email is user-provided; sending it to that address is the intended behavior.
        await _emailService.SendEmailAsync(normalizedEmail, subject, body, cancellationToken);

        _logger.LogInformation("Invitation sent for project {ProjectId}", projectId);

        return invitation;
    }

    public async Task<Project?> AcceptInvitationAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.ProjectInvitations
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invitation is null || invitation.ExpiresAt < DateTime.UtcNow || invitation.AcceptedAt is not null)
            return null;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            return null;

        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.Project.PartnerId = userId;

        await _context.SaveChangesAsync(cancellationToken);
        return invitation.Project;
    }

    public async Task<Project?> GetInvitationProjectAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.ProjectInvitations
            .Include(i => i.Project)
            .ThenInclude(p => p.Goals)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invitation is null || invitation.ExpiresAt < DateTime.UtcNow)
            return null;

        return invitation.Project;
    }

    public async Task<Project?> GetProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // AsNoTracking: this read feeds display-only pages that poll on a long-lived
        // Blazor circuit (spec RT2). Without it, EF's identity map would return the
        // stale tracked instance instead of the current database state.
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Goals)
            .Include(p => p.ChangeRequests)
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return null;

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        return project;
    }

    public async Task<List<Project>> GetUserProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Goals and both partners are eagerly loaded because the project list card shows
        // the goal count and the pair's avatars.
        return await _context.Projects
            .AsNoTracking()
            .Include(p => p.Goals)
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .Where(p => p.CreatorId == userId || p.PartnerId == userId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task ApproveProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        if (project.PartnerId is null)
            throw new InvalidOperationException("Project has no partner yet.");

        project.Status = ProjectStatus.Active;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectChangeRequest> ProposeChangeAsync(
        Guid projectId,
        Guid userId,
        ChangeRequestType type,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        if (project.Status is ProjectStatus.Finished or ProjectStatus.Cancelled)
            throw new InvalidOperationException("Cannot propose changes to a finished or cancelled project.");

        var hasPending = await _context.ProjectChangeRequests
            .AnyAsync(cr => cr.ProjectId == projectId && cr.Status == ChangeRequestStatus.Pending, cancellationToken);

        if (hasPending)
            throw new InvalidOperationException("There is already a pending change request for this project.");

        var changeRequest = new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RequestedByUserId = userId,
            Type = type,
            PayloadJson = payloadJson,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectChangeRequests.Add(changeRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return changeRequest;
    }

    public async Task RespondToChangeRequestAsync(
        Guid projectId,
        Guid changeRequestId,
        Guid userId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(p => p.Goals)
            .ThenInclude(g => g.Metrics)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        var changeRequest = await _context.ProjectChangeRequests
            .FirstOrDefaultAsync(cr => cr.Id == changeRequestId && cr.ProjectId == projectId, cancellationToken);

        if (changeRequest is null)
            throw new ArgumentException("Change request not found.");

        if (changeRequest.Status != ChangeRequestStatus.Pending)
            throw new InvalidOperationException("Change request is no longer pending.");

        if (changeRequest.RequestedByUserId == userId)
            throw new UnauthorizedAccessException("You cannot respond to your own change request.");

        if (approve)
        {
            changeRequest.Status = ChangeRequestStatus.Approved;
            ApplyChangeRequest(project, changeRequest);
        }
        else
        {
            changeRequest.Status = ChangeRequestStatus.Rejected;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void ApplyChangeRequest(Project project, ProjectChangeRequest changeRequest)
    {
        switch (changeRequest.Type)
        {
            case ChangeRequestType.EndDate:
                var endDatePayload = DeserializePayload<EndDatePayload>(changeRequest);
                project.EndDate = DateTime.SpecifyKind(endDatePayload.EndDate, DateTimeKind.Utc);
                break;

            case ChangeRequestType.Frequency:
                var freqPayload = DeserializePayload<FrequencyPayload>(changeRequest);
                project.Frequency = freqPayload.Frequency;
                break;

            case ChangeRequestType.Goals:
                var goalsPayload = DeserializePayload<GoalsPayload>(changeRequest);
                ApplyGoalChanges(project, goalsPayload);
                break;

            default:
                throw new InvalidOperationException($"Unsupported change request type '{changeRequest.Type}'.");
        }
    }

    private void ApplyGoalChanges(Project project, GoalsPayload goalsPayload)
    {
        var requestedGoals = goalsPayload.Goals ?? new List<GoalPayloadItem>();
        var existingByLabel = project.Goals.ToDictionary(
            g => g.Label,
            g => g,
            StringComparer.OrdinalIgnoreCase);

        var processedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requested in requestedGoals)
        {
            processedLabels.Add(requested.Label);

            EnsureValidDefinition(
                requested.Label, requested.DataType, requested.MinValue, requested.MaxValue, requested.TargetValue);

            if (existingByLabel.TryGetValue(requested.Label, out var existing))
            {
                // Preserve the existing goal (and its check-in history) while
                // updating its definition to the requested values.
                existing.DataType = requested.DataType;
                existing.Unit = requested.Unit;
                existing.MinValue = Normalize(requested.DataType, requested.MinValue);
                existing.MaxValue = Normalize(requested.DataType, requested.MaxValue);
                existing.TargetValue = Normalize(requested.DataType, requested.TargetValue);
            }
            else
            {
                var goalField = new GoalField
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Label = requested.Label,
                    DataType = requested.DataType,
                    Unit = requested.Unit,
                    MinValue = Normalize(requested.DataType, requested.MinValue),
                    MaxValue = Normalize(requested.DataType, requested.MaxValue),
                    TargetValue = Normalize(requested.DataType, requested.TargetValue)
                };
                _context.GoalFields.Add(goalField);
                project.Goals.Add(goalField);
            }
        }

        // Remove only goals that are no longer requested and have no check-in
        // history. If a goal has historical data we keep it to preserve metrics.
        var goalsToRemove = project.Goals
            .Where(g => !processedLabels.Contains(g.Label))
            .Where(g => !g.Metrics.Any())
            .ToList();

        foreach (var goal in goalsToRemove)
        {
            project.Goals.Remove(goal);
            _context.GoalFields.Remove(goal);
        }
    }

    /// <summary>
    /// A goal whose bounds or target break its own data type is unfillable, so the
    /// definition is rejected at the door (spec X2).
    /// </summary>
    private static void EnsureValidDefinition(
        string label, GoalDataType dataType, decimal? minValue, decimal? maxValue, decimal? targetValue)
    {
        if (GoalValueRules.ValidateDefinition(dataType, minValue, maxValue, targetValue) is { } error)
            throw new GoalValueException(error, dataType, label, minValue, maxValue);
    }

    private static decimal? Normalize(GoalDataType dataType, decimal? value) =>
        value is { } present ? GoalValueRules.Normalize(dataType, present) : null;

    public async Task<(int Current, int Best)> GetStreakAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        var periods = await _context.CheckIns
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId && c.UserId == userId)
            .OrderByDescending(c => c.PeriodNumber)
            .Select(c => c.PeriodNumber)
            .ToListAsync(cancellationToken);

        return StreakCalculator.FromPeriods(periods);
    }

    private T DeserializePayload<T>(ProjectChangeRequest changeRequest) where T : class
    {
        try
        {
            var payload = JsonSerializer.Deserialize<T>(changeRequest.PayloadJson);
            if (payload is null)
                throw new InvalidOperationException(
                    $"Change request {changeRequest.Id} has an empty payload for type '{changeRequest.Type}'.");

            return payload;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize payload for change request {ChangeRequestId} of type {ChangeRequestType}",
                changeRequest.Id, changeRequest.Type);

            throw new InvalidOperationException(
                $"Change request {changeRequest.Id} has a malformed payload for type '{changeRequest.Type}'.", ex);
        }
    }

}

internal sealed class EndDatePayload
{
    public DateTime EndDate { get; set; }
}

internal sealed class FrequencyPayload
{
    public ProjectFrequency Frequency { get; set; }
}

internal sealed class GoalsPayload
{
    public List<GoalPayloadItem> Goals { get; set; } = new();
}

internal sealed class GoalPayloadItem
{
    public string Label { get; set; } = null!;
    public GoalDataType DataType { get; set; }
    public string Unit { get; set; } = null!;
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? TargetValue { get; set; }
}
