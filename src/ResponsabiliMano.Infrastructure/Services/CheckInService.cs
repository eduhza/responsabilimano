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
        var existing = period >= 1
            ? await _context.CheckIns
                .Include(c => c.Metrics)
                .FirstOrDefaultAsync(
                    c => c.ProjectId == projectId && c.UserId == userId && c.PeriodNumber == period,
                    cancellationToken)
            : null;

        var alreadySubmitted = existing is not null;

        var periodEnd = period >= 1
            ? PeriodCalculator.PeriodEnd(project.StartDate, project.Frequency, period, project.EndDate)
            : DateTime.MinValue;

        return new CheckInForm(project, period, alreadySubmitted, periodEnd, existing);
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

        var existingByProject = (await _context.CheckIns
            .AsNoTracking()
            .Include(c => c.Metrics)
            .Where(c => c.UserId == userId && projectIds.Contains(c.ProjectId))
            .ToListAsync(cancellationToken))
            .ToLookup(c => c.ProjectId);

        var forms = new List<CheckInForm>(projects.Count);

        foreach (var project in projects)
        {
            var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, now);
            if (period < 1)
                continue;

            var existing = existingByProject[project.Id]
                .FirstOrDefault(c => c.PeriodNumber == period);
            var alreadySubmitted = existing is not null;
            var periodEnd = PeriodCalculator.PeriodEnd(
                project.StartDate, project.Frequency, period, project.EndDate);

            forms.Add(new CheckInForm(project, period, alreadySubmitted, periodEnd, existing));
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

    public async Task<CheckIn?> UpdateCurrentCheckInAsync(
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
            return null;

        EnsureParticipant(project, userId);

        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Check-ins are only allowed for active projects.");

        var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, DateTime.UtcNow);
        if (period < 1)
            throw new InvalidOperationException("The project has not started yet.");

        var existing = await FindCurrentCheckInAsync(projectId, userId, period, cancellationToken);

        if (existing.PeriodNumber != period)
            throw new InvalidOperationException("Only the current period's check-in can be edited.");

        if (existing.UserId != userId)
            throw new UnauthorizedAccessException("You can only edit your own check-in.");

        var validatedMetrics = ValidateMetrics(project, metrics);

        existing.Feeling = feeling;
        existing.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _context.CheckInMetrics.RemoveRange(existing.Metrics.ToList());
        existing.Metrics.Clear();

        foreach (var metric in validatedMetrics)
        {
            _context.CheckInMetrics.Add(metric);
            existing.Metrics.Add(metric);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Check-in {CheckInId} updated for project {ProjectId}, user {UserId}, period {Period}",
            existing.Id, projectId, userId, period);

        return existing;
    }

    public async Task<bool> DeleteCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return false;

        EnsureParticipant(project, userId);

        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Check-ins are only allowed for active projects.");

        var period = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, DateTime.UtcNow);
        if (period < 1)
            throw new InvalidOperationException("The project has not started yet.");

        var existing = await FindCurrentCheckInAsync(projectId, userId, period, cancellationToken);

        if (existing.PeriodNumber != period)
            throw new InvalidOperationException("Only the current period's check-in can be deleted.");

        if (existing.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own check-in.");

        _context.CheckIns.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Check-in {CheckInId} deleted for project {ProjectId}, user {UserId}, period {Period}",
            existing.Id, projectId, userId, period);

        return true;
    }

    /// <summary>
    /// Loads the check-in for the caller's current period, enforcing ownership and
    /// existence. Throws <see cref="ArgumentException"/> when no check-in exists for
    /// the current period, and <see cref="UnauthorizedAccessException"/> when the only
    /// check-in in the period belongs to another participant.
    /// </summary>
    private async Task<CheckIn> FindCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        int period,
        CancellationToken cancellationToken)
    {
        var checkIns = await _context.CheckIns
            .Include(c => c.Metrics)
            .Where(c => c.ProjectId == projectId && c.PeriodNumber == period)
            .ToListAsync(cancellationToken);

        var existing = checkIns.FirstOrDefault(c => c.UserId == userId);

        if (existing is null)
        {
            if (checkIns.Any(c => c.UserId != userId))
                throw new UnauthorizedAccessException("This check-in belongs to another participant.");

            throw new ArgumentException("No check-in found for the current period.");
        }

        return existing;
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

            // Server-side guard for the data type and bounds (spec X2). The UI runs the
            // same GoalValueRules, but a direct API call must not get past this.
            if (GoalValueRules.Validate(goal.DataType, metric.Value, goal.MinValue, goal.MaxValue) is { } error)
                throw new GoalValueException(error, goal.DataType, goal.Label, goal.MinValue, goal.MaxValue);

            result.Add(new CheckInMetric
            {
                Id = Guid.NewGuid(),
                GoalFieldId = metric.GoalFieldId,
                Value = GoalValueRules.Normalize(goal.DataType, metric.Value)
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
