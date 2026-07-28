namespace ResponsabiliMano.Core.Services;

/// <summary>
/// Periodic check-in notifications, driven by an external scheduler (specs S3.3
/// and S3.4). Both operations are idempotent within a period: re-running them
/// sends nothing that was already sent.
/// </summary>
public interface ICheckInNotificationService
{
    /// <summary>
    /// Sends the "fill in your check-in" email to every participant of every
    /// active, not-yet-ended project for its current period (spec S3.3).
    /// </summary>
    /// <returns>The number of emails sent on this run.</returns>
    Task<int> DispatchCheckInEmailsAsync(
        DateTime nowUtc,
        string baseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a reminder to each participant who has no check-in for the current
    /// period of an active, not-yet-ended project (spec S3.4).
    /// </summary>
    /// <returns>The number of reminders sent on this run.</returns>
    Task<int> DispatchRemindersAsync(
        DateTime nowUtc,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
