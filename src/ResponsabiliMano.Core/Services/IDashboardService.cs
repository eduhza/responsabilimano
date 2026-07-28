using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Services;

/// <summary>
/// Aggregated dashboard data for a project (spec S4.1). Returns participants
/// with their latest feeling, time-series per goal field, and per-user averages.
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
}

public sealed record DashboardResponse(
    Guid ProjectId,
    string ProjectName,
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
    decimal? TargetValue,
    List<DashboardSeriesEntry> Series);

public sealed record DashboardSeriesEntry(
    Guid UserId,
    int PeriodNumber,
    DateTime SubmittedAt,
    decimal Value,
    decimal? AverageValue);
