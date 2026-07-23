using System.Security.Cryptography;
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
            StartDate = startDate,
            EndDate = endDate,
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

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
