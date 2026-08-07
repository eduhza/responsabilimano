using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(AppDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardResponse?> GetDashboardAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // AsNoTracking: the dashboard is display-only and polled on a long-lived
        // Blazor circuit (spec RT2); avoid EF's identity map returning stale state.
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Goals)
            .ThenInclude(g => g.Targets)
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            return null;

        EnsureParticipant(project, userId);

        Guid[] participantIds = project.PartnerId is { } partnerId
            ? [project.CreatorId, partnerId]
            : [project.CreatorId];

        var participantNames = new Dictionary<Guid, string>
        {
            [project.CreatorId] = project.Creator.Name
        };
        if (project.Partner is not null)
            participantNames[project.Partner.Id] = project.Partner.Name;

        var checkIns = await LoadCheckInsAsync(projectId, cancellationToken);

        var participants = participantIds
            .Select(pid => new DashboardParticipant(
                pid,
                participantNames.GetValueOrDefault(pid, ""),
                LatestFeelingFor(checkIns, pid)))
            .ToList();

        var metrics = project.Goals
            .Select(goal => new DashboardMetricSeries(
                goal.Id,
                goal.Label,
                goal.Unit,
                goal.DataType,
                BuildTargets(goal, participantIds),
                BuildSeries(checkIns, goal.Id, participantIds)))
            .ToList();

        _logger.LogInformation(
            "Dashboard built for project {ProjectId} requested by user {UserId}",
            projectId, userId);

        return new DashboardResponse(
            project.Id,
            project.Name,
            PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, DateTime.UtcNow),
            PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, project.EndDate),
            participants,
            metrics);
    }

    public async Task<GlobalDashboardResponse> GetGlobalDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Two queries, never one per project: the panel polls every few seconds
        // (spec RT2) and a user can carry any number of projects.
        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Goals)
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .Where(p => p.CreatorId == userId || p.PartnerId == userId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return new GlobalDashboardResponse(0, 0, 0, 0, 0, 0, []);
        }

        var projectIds = projects.Select(p => p.Id).ToList();

        var myCheckIns = await _context.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId && projectIds.Contains(c.ProjectId))
            .Select(c => new { c.ProjectId, c.PeriodNumber, c.Feeling })
            .ToListAsync(cancellationToken);

        var byProject = myCheckIns
            .GroupBy(c => c.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        var summaries = new List<GlobalProjectSummary>(projects.Count);
        var currentStreak = 0;
        var bestStreak = 0;

        foreach (var project in projects)
        {
            var mine = byProject.GetValueOrDefault(project.Id, []);

            var currentPeriod = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, now);
            // The end date lands inside the last period, so the same calculation
            // over it yields how many periods the project has in total.
            var totalPeriods = PeriodCalculator.CurrentPeriod(project.StartDate, project.Frequency, project.EndDate);

            var streak = StreakCalculator.FromPeriods(mine.Select(c => c.PeriodNumber));
            currentStreak = Math.Max(currentStreak, streak.Current);
            bestStreak = Math.Max(bestStreak, streak.Best);

            var pending = project.Status == ProjectStatus.Active
                && currentPeriod > 0
                && mine.All(c => c.PeriodNumber != currentPeriod);

            summaries.Add(new GlobalProjectSummary(
                project.Id,
                project.Name,
                project.Icon,
                project.Status,
                project.StartDate,
                project.EndDate,
                project.Frequency,
                project.Creator.Name,
                project.Partner?.Name,
                currentPeriod,
                totalPeriods,
                mine.Count,
                pending,
                mine.Count == 0 ? null : mine.MaxBy(c => c.PeriodNumber)!.Feeling,
                project.Goals.Count));
        }

        _logger.LogInformation(
            "Global dashboard built for user {UserId} over {ProjectCount} projects",
            userId, projects.Count);

        return new GlobalDashboardResponse(
            summaries.Count,
            summaries.Count(s => s.Status == ProjectStatus.Active),
            myCheckIns.Count,
            currentStreak,
            bestStreak,
            summaries.Count(s => s.CheckInPending),
            summaries);
    }

    private async Task<List<CheckInSnapshot>> LoadCheckInsAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.CheckIns
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.PeriodNumber)
            .Select(c => new CheckInSnapshot(
                c.UserId,
                c.PeriodNumber,
                c.SubmittedAt,
                c.Feeling,
                c.Metrics.Select(m => new MetricSnapshot(m.GoalFieldId, m.Value)).ToList()))
            .ToListAsync(cancellationToken);
    }

    private static Feeling? LatestFeelingFor(
        List<CheckInSnapshot> checkIns, Guid userId)
    {
        return checkIns
            .Where(c => c.UserId == userId)
            .MaxBy(c => c.PeriodNumber)?.Feeling;
    }

    private static List<DashboardSeriesEntry> BuildSeries(
        List<CheckInSnapshot> checkIns, Guid goalFieldId, Guid[] participantIds)
    {
        var entries = new List<DashboardSeriesEntry>();

        foreach (var pid in participantIds)
        {
            var valuesForGoal = checkIns
                .Where(c => c.UserId == pid)
                .SelectMany(c => c.Metrics)
                .Where(m => m.GoalFieldId == goalFieldId)
                .Select(m => m.Value)
                .ToList();

            decimal? average = valuesForGoal.Count > 0
                ? Math.Round(valuesForGoal.Average(), 2)
                : null;

            foreach (var checkIn in checkIns.Where(c => c.UserId == pid).OrderBy(c => c.PeriodNumber))
            {
                foreach (var metric in checkIn.Metrics.Where(m => m.GoalFieldId == goalFieldId))
                {
                    entries.Add(new DashboardSeriesEntry(
                        pid,
                        checkIn.PeriodNumber,
                        checkIn.SubmittedAt,
                        metric.Value,
                        average));
                }
            }
        }

        return entries
            .OrderBy(e => e.PeriodNumber)
            .ToList();
    }

    private static List<DashboardMetricTarget> BuildTargets(GoalField goal, Guid[] participantIds)
    {
        return participantIds
            .Select(pid =>
            {
                var target = goal.Targets.FirstOrDefault(t => t.UserId == pid);
                return target is not null
                    ? new DashboardMetricTarget(pid, target.Baseline, target.TargetValue, target.Direction)
                    : new DashboardMetricTarget(pid, null, null, GoalDirection.Reach);
            })
            .ToList();
    }

    private static void EnsureParticipant(Project project, Guid userId)
    {
        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");
    }

    private sealed record CheckInSnapshot(
        Guid UserId,
        int PeriodNumber,
        DateTime SubmittedAt,
        Feeling Feeling,
        List<MetricSnapshot> Metrics);

    private sealed record MetricSnapshot(Guid GoalFieldId, decimal Value);
}
