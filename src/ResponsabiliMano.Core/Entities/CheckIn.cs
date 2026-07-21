using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class CheckIn
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public Feeling Feeling { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PeriodNumber { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<CheckInMetric> Metrics { get; set; } = new List<CheckInMetric>();
}
