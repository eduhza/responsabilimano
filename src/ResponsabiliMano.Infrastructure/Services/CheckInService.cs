using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class CheckInService : ICheckInService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CheckInService> _logger;

    public CheckInService(AppDbContext context, ILogger<CheckInService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CheckInForm?> GetCheckInFormAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(p => p.Goals)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return null;

        EnsureParticipant(project, userId);

        var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, DateTime.UtcNow);
        var alreadySubmitted = period >= 1 && await _context.CheckIns.AnyAsync(
            c => c.ProjectId == projectId && c.UserId == userId && c.PeriodNumber == period,
            cancellationToken);

        var periodEnd = period >= 1
            ? PeriodCalculator.PeriodEnd(project.StartDate, project.Frequency, period, project.EndDate)
            : DateTime.MinValue;

        return new CheckInForm(project, period, alreadySubmitted, periodEnd);
    }

    public async Task<IReadOnlyList<CheckInForm>> GetCheckInFormsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Goals)
            .Where(p => p.Status == ProjectStatus.Active
                && p.StartDate.Date <= now.Date
                && p.EndDate.Date >= now.Date
                && (p.CreatorId == userId || p.PartnerId == userId))
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
            return [];

        var projectIds = projects.Select(p => p.Id).ToList();

        var submittedPeriods = await _context.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId && projectIds.Contains(c.ProjectId))
            .Select(c => new { c.ProjectId, c.PeriodNumber })
            .ToListAsync(cancellationToken);

        var submittedSet = new HashSet<(Guid ProjectId, int Period)>(
            submittedPeriods.Select(c => (c.ProjectId, c.PeriodNumber)));

        var forms = new List<CheckInForm>(projects.Count);

        foreach (var project in projects)
        {
            var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, now);
            if (period < 1)
                continue;

            var alreadySubmitted = submittedSet.Contains((project.Id, period));
            var periodEnd = PeriodCalculator.PeriodEnd(
                project.StartDate, project.Frequency, period, project.EndDate);

            forms.Add(new CheckInForm(project, period, alreadySubmitted, periodEnd));
        }

        return forms;
    }

    public async Task<CheckIn> SubmitCheckInAsync(
        Guid projectId,
        Guid userId,
        Feeling feeling,
        IReadOnlyCollection<CheckInMetricInput> metrics,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Include(p => p.Goals)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        EnsureParticipant(project, userId);

        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Check-ins are only allowed for active projects.");

        var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, DateTime.UtcNow);
        if (period < 1)
            throw new InvalidOperationException("The project has not started yet.");

        var alreadySubmitted = await _context.CheckIns.AnyAsync(
            c => c.ProjectId == projectId && c.UserId == userId && c.PeriodNumber == period,
            cancellationToken);
        if (alreadySubmitted)
            throw new InvalidOperationException("A check-in for this period has already been submitted.");

        var validatedMetrics = ValidateMetrics(project, metrics);

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = feeling,
            PeriodNumber = period,
            SubmittedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Metrics = validatedMetrics
        };

        _context.CheckIns.Add(checkIn);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Check-in {CheckInId} recorded for project {ProjectId}, user {UserId}, period {Period}",
            checkIn.Id, projectId, userId, period);

        return checkIn;
    }

    private static List<CheckInMetric> ValidateMetrics(Project project, IReadOnlyCollection<CheckInMetricInput> metrics)
    {
        if (metrics is null || metrics.Count == 0)
            throw new ArgumentException("At least one metric value is required.");

        var goalsById = project.Goals.ToDictionary(g => g.Id);
        var seen = new HashSet<Guid>();
        var result = new List<CheckInMetric>(metrics.Count);

        foreach (var metric in metrics)
        {
            if (!goalsById.TryGetValue(metric.GoalFieldId, out var goal))
                throw new ArgumentException($"Goal field '{metric.GoalFieldId}' does not belong to this project.");

            if (!seen.Add(metric.GoalFieldId))
                throw new ArgumentException($"Duplicate value for goal field '{metric.GoalFieldId}'.");

            if (goal.MinValue is { } min && metric.Value < min)
                throw new ArgumentException($"Value for '{goal.Label}' is below the minimum of {min}.");

            if (goal.MaxValue is { } max && metric.Value > max)
                throw new ArgumentException($"Value for '{goal.Label}' is above the maximum of {max}.");

            result.Add(new CheckInMetric
            {
                Id = Guid.NewGuid(),
                GoalFieldId = metric.GoalFieldId,
                Value = metric.Value
            });
        }

        return result;
    }

    private static void EnsureParticipant(Project project, Guid userId)
    {
        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");
    }
}
