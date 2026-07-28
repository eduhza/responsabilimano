using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class CheckInNotificationService : ICheckInNotificationService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CheckInNotificationService> _logger;

    public CheckInNotificationService(
        AppDbContext context,
        IEmailService emailService,
        ILogger<CheckInNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<int> DispatchCheckInEmailsAsync(
        DateTime nowUtc,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var projects = await LoadEligibleProjectsAsync(nowUtc, cancellationToken);
        var sent = 0;

        foreach (var project in projects)
        {
            var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, nowUtc);
            if (period < 1)
                continue;

            foreach (var user in Participants(project))
            {
                if (await AlreadyNotifiedAsync(project.Id, user.Id, period, CheckInNotificationKind.CheckInRequest, cancellationToken))
                    continue;

                await SendCheckInRequestEmailAsync(user, project, baseUrl, cancellationToken);
                RecordNotification(project.Id, user.Id, period, CheckInNotificationKind.CheckInRequest, nowUtc);
                sent++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Check-in dispatch sent {Count} email(s) across {ProjectCount} project(s).", sent, projects.Count);
        return sent;
    }

    public async Task<int> DispatchRemindersAsync(
        DateTime nowUtc,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var projects = await LoadEligibleProjectsAsync(nowUtc, cancellationToken);
        var sent = 0;

        foreach (var project in projects)
        {
            var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, nowUtc);
            if (period < 1)
                continue;

            foreach (var user in Participants(project))
            {
                var hasCheckIn = await _context.CheckIns.AnyAsync(
                    c => c.ProjectId == project.Id && c.UserId == user.Id && c.PeriodNumber == period,
                    cancellationToken);
                if (hasCheckIn)
                    continue;

                if (await AlreadyNotifiedAsync(project.Id, user.Id, period, CheckInNotificationKind.Reminder, cancellationToken))
                    continue;

                await SendReminderEmailAsync(user, project, baseUrl, cancellationToken);
                RecordNotification(project.Id, user.Id, period, CheckInNotificationKind.Reminder, nowUtc);
                sent++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Reminder dispatch sent {Count} reminder(s) across {ProjectCount} project(s).", sent, projects.Count);
        return sent;
    }

    private async Task<List<Project>> LoadEligibleProjectsAsync(DateTime nowUtc, CancellationToken cancellationToken)
        => await _context.Projects
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .Where(p => p.Status == ProjectStatus.Active && p.EndDate >= nowUtc)
            .ToListAsync(cancellationToken);

    private Task<bool> AlreadyNotifiedAsync(
        Guid projectId, Guid userId, int period, CheckInNotificationKind kind, CancellationToken cancellationToken)
        => _context.CheckInNotifications.AnyAsync(
            n => n.ProjectId == projectId && n.UserId == userId && n.PeriodNumber == period && n.Kind == kind,
            cancellationToken);

    private void RecordNotification(Guid projectId, Guid userId, int period, CheckInNotificationKind kind, DateTime nowUtc)
        => _context.CheckInNotifications.Add(new CheckInNotification
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            PeriodNumber = period,
            Kind = kind,
            SentAt = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc)
        });

    private static IEnumerable<User> Participants(Project project)
    {
        yield return project.Creator;
        if (project.Partner is not null)
            yield return project.Partner;
    }

    private Task SendCheckInRequestEmailAsync(User user, Project project, string baseUrl, CancellationToken cancellationToken)
    {
        var link = CheckInLink(baseUrl, project.Id);
        var subject = "Hora do check-in - ResponsabiliMano";
        var body = $"""
            <h2>Hora do check-in!</h2>
            <p>Ola, {user.Name}. Chegou a hora de registrar seu check-in do projeto "{project.Name}".</p>
            <p>Clique no link abaixo para preencher:</p>
            <p><a href="{link}">{link}</a></p>
            """;
        return _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    private Task SendReminderEmailAsync(User user, Project project, string baseUrl, CancellationToken cancellationToken)
    {
        var link = CheckInLink(baseUrl, project.Id);
        var subject = "Lembrete de check-in - ResponsabiliMano";
        var body = $"""
            <h2>Voce ainda nao fez seu check-in</h2>
            <p>Ola, {user.Name}. Este e um lembrete para registrar seu check-in do projeto "{project.Name}".</p>
            <p>Clique no link abaixo para preencher:</p>
            <p><a href="{link}">{link}</a></p>
            """;
        return _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
    }

    private static string CheckInLink(string baseUrl, Guid projectId)
        => $"{baseUrl.TrimEnd('/')}/projects/{projectId}/checkin";
}
