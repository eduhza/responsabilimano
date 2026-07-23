using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

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
        CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
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

            project.Goals.Add(new GoalField
            {
                Id = Guid.NewGuid(),
                Label = goal.Label.Trim(),
                DataType = goal.DataType,
                Unit = goal.Unit.Trim(),
                MinValue = goal.MinValue,
                MaxValue = goal.MaxValue,
                TargetValue = goal.TargetValue
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

        var normalizedEmail = partnerEmail.Trim().ToLowerInvariant();

        if (project.CreatorId == await _context.Users
            .Where(u => u.Email.ToLower() == normalizedEmail)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken))
        {
            throw new ArgumentException("Cannot invite yourself.");
        }

        var token = GenerateToken();
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
        var subject = "Convite - ResponsabiliMano";
        var body = $"""
            <h2>Voce foi convidado!</h2>
            <p>Ola! Voce foi convidado para participar do projeto "{project.Name}" no ResponsabiliMano.</p>
            <p>Clique no link abaixo para visualizar o projeto e aceitar o convite:</p>
            <p><a href="{inviteLink}">{inviteLink}</a></p>
            <p>Este convite expira em 7 dias.</p>
            """;

        await _emailService.SendEmailAsync(normalizedEmail, subject, body, cancellationToken);

        _logger.LogInformation("Invitation sent for project {ProjectId} to {Email}", projectId, normalizedEmail);

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
        var project = await _context.Projects
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
        return await _context.Projects
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
                var endDatePayload = JsonSerializer.Deserialize<EndDatePayload>(changeRequest.PayloadJson);
                if (endDatePayload is not null)
                    project.EndDate = DateTime.SpecifyKind(endDatePayload.EndDate, DateTimeKind.Utc);
                break;

            case ChangeRequestType.Frequency:
                var freqPayload = JsonSerializer.Deserialize<FrequencyPayload>(changeRequest.PayloadJson);
                if (freqPayload is not null)
                    project.Frequency = freqPayload.Frequency;
                break;

            case ChangeRequestType.Goals:
                var goalsPayload = JsonSerializer.Deserialize<GoalsPayload>(changeRequest.PayloadJson);
                if (goalsPayload is not null)
                {
                    _context.GoalFields.RemoveRange(project.Goals);
                    project.Goals.Clear();
                    foreach (var g in goalsPayload.Goals)
                    {
                        var goalField = new GoalField
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Id,
                            Label = g.Label,
                            DataType = g.DataType,
                            Unit = g.Unit,
                            MinValue = g.MinValue,
                            MaxValue = g.MaxValue,
                            TargetValue = g.TargetValue
                        };
                        _context.GoalFields.Add(goalField);
                        project.Goals.Add(goalField);
                    }
                }
                break;
        }
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
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
