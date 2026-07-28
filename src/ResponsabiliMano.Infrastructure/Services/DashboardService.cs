using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        var project = await _context.Projects
            .Include(p => p.Goals)
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
                goal.TargetValue,
                BuildSeries(checkIns, goal.Id, participantIds)))
            .ToList();

        _logger.LogInformation(
            "Dashboard built for project {ProjectId} requested by user {UserId}",
            projectId, userId);

        return new DashboardResponse(project.Id, project.Name, participants, metrics);
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
