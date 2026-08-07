using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Services;

/// <summary>
/// Check-in capture for the current period of an active project (specs S3.1/S3.2).
/// The current period is always derived server-side from the project frequency —
/// never trusted from the client.
/// </summary>
public interface ICheckInService
{
    /// <summary>
    /// Loads the data a participant needs to fill in the current period's check-in:
    /// the project (with goals), the current period number and whether the caller
    /// already submitted. Returns <c>null</c> when the project does not exist.
    /// Throws <see cref="UnauthorizedAccessException"/> when the caller is not a
    /// participant.
    /// </summary>
    Task<CheckInForm?> GetCheckInFormAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the current-period check-in form for every active project the caller
    /// participates in that has already started and has not ended. The list is
    /// unsorted; callers are expected to order it for their own UI.
    /// </summary>
    Task<IReadOnlyList<CheckInForm>> GetCheckInFormsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a check-in (and its metrics) for the caller's current period.
    /// </summary>
    /// <exception cref="ArgumentException">Project/metric invalid (400).</exception>
    /// <exception cref="UnauthorizedAccessException">Caller is not a participant (403).</exception>
    /// <exception cref="InvalidOperationException">
    /// Project not active, not started, or already submitted this period (409).
    /// </exception>
    Task<CheckIn> SubmitCheckInAsync(
        Guid projectId,
        Guid userId,
        Feeling feeling,
        IReadOnlyCollection<CheckInMetricInput> metrics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the current period's check-in for the caller with new feeling and metrics.
    /// Returns <c>null</c> when the project does not exist.
    /// </summary>
    /// <exception cref="ArgumentException">No check-in in the current period (400).</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Caller is not a participant, or the check-in belongs to another user (403).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Project not active, not started, or the check-in is for a past period (409).
    /// </exception>
    Task<CheckIn?> UpdateCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        Feeling feeling,
        IReadOnlyCollection<CheckInMetricInput> metrics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the current period's check-in for the caller.
    /// Returns <c>false</c> when the project does not exist.
    /// </summary>
    /// <exception cref="ArgumentException">No check-in in the current period (400).</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Caller is not a participant, or the check-in belongs to another user (403).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Project not active, not started, or the check-in is for a past period (409).
    /// </exception>
    Task<bool> DeleteCurrentCheckInAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>A single metric value the participant reports for one goal field.</summary>
public sealed record CheckInMetricInput(Guid GoalFieldId, decimal Value);

/// <summary>The state a check-in form needs to render for the current period.</summary>
public sealed record CheckInForm(
    Project Project,
    int PeriodNumber,
    bool AlreadySubmitted,
    DateTime PeriodEnd = default,
    CheckIn? Existing = null);
