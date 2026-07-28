using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Web.Models;

/// <summary>
/// Payload for <c>POST /api/projects/{id}/checkins</c> (spec S3.2). The period is
/// never sent by the client — it is derived server-side from the project frequency.
/// </summary>
public sealed class SubmitCheckInRequest
{
    public Feeling Feeling { get; set; } = Feeling.Neutral;
    public List<CheckInMetricValue> Metrics { get; set; } = new();
}

public sealed class CheckInMetricValue
{
    public Guid GoalFieldId { get; set; }
    public decimal Value { get; set; }
}
