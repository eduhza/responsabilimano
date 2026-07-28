using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

/// <summary>
/// Records that a check-in notification (request or reminder) has been sent to a
/// participant for a given project period. Its unique key
/// (ProjectId, UserId, PeriodNumber, Kind) makes the notification jobs idempotent
/// (specs S3.3, S3.4): re-running a job in the same period sends nothing new.
/// </summary>
public class CheckInNotification
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public int PeriodNumber { get; set; }
    public CheckInNotificationKind Kind { get; set; }
    public DateTime SentAt { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
