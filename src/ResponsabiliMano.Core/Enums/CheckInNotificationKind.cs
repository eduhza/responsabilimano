namespace ResponsabiliMano.Core.Enums;

/// <summary>
/// Kind of check-in notification sent to a participant in a given period. Used to
/// keep the dispatch (spec S3.3) and the reminder (spec S3.4) jobs idempotent:
/// at most one notification of each kind per (project, user, period).
/// </summary>
public enum CheckInNotificationKind
{
    /// <summary>The periodic "please fill in your check-in" email (S3.3).</summary>
    CheckInRequest = 1,

    /// <summary>The follow-up reminder for a participant who has not submitted yet (S3.4).</summary>
    Reminder = 2
}
