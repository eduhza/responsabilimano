using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Services;

/// <summary>
/// Aggregated dashboard data: per project (spec S4.1) — participants with their
/// latest feeling, time-series per goal field and per-user averages — and
/// account-wide (spec S6.1) — consolidated counters plus one card per project.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Builds the dashboard response for the given project. Returns <c>null</c>
    /// when the project does not exist. Throws <see cref="UnauthorizedAccessException"/>
    /// when the caller is not a participant.
    /// </summary>
    Task<DashboardResponse?> GetDashboardAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the account-wide panel (spec S6.1): consolidated counters across every
    /// project the caller participates in, plus one summary per project. Never
    /// <c>null</c> — a user with no projects gets an empty payload, which is what the
    /// page renders as its empty state.
    /// </summary>
    Task<GlobalDashboardResponse> GetGlobalDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record DashboardResponse(
    Guid ProjectId,
    string ProjectName,
    int CurrentPeriod,
    int TotalPeriods,
    List<DashboardParticipant> Participants,
    List<DashboardMetricSeries> Metrics);

public sealed record DashboardParticipant(
    Guid UserId,
    string Name,
    Feeling? LatestFeeling);

public sealed record DashboardMetricSeries(
    Guid GoalFieldId,
    string Label,
    string Unit,
    GoalDataType DataType,
    decimal? MinValue,
    decimal? MaxValue,
    List<DashboardMetricTarget> Targets,
    List<DashboardSeriesEntry> Series);

public sealed record DashboardMetricTarget(
    Guid UserId,
    decimal? Baseline,
    decimal? TargetValue,
    GoalDirection Direction);

public sealed record DashboardSeriesEntry(
    Guid UserId,
    int PeriodNumber,
    DateTime SubmittedAt,
    decimal Value,
    decimal? AverageValue);

/// <summary>
/// Account-wide panel (spec S6.1). <c>CurrentStreak</c>/<c>BestStreak</c> are the
/// caller's best across projects, not a sum: a streak only means something inside
/// one project's cadence.
/// </summary>
public sealed record GlobalDashboardResponse(
    int TotalProjects,
    int ActiveProjects,
    int TotalCheckIns,
    int CurrentStreak,
    int BestStreak,
    int OpenCheckIns,
    List<GlobalProjectSummary> Projects);

public sealed record GlobalProjectSummary(
    Guid ProjectId,
    string Name,
    string? Icon,
    ProjectStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    ProjectFrequency Frequency,
    string CreatorName,
    string? PartnerName,
    int CurrentPeriod,
    int TotalPeriods,
    int CheckInsSubmitted,
    bool CheckInPending,
    Feeling? LatestFeeling,
    int GoalCount);
