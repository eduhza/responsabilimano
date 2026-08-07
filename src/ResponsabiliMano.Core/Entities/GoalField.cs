using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class GoalField
{
    public Guid Id { get; set; }
    public string Label { get; set; } = null!;
    public GoalDataType DataType { get; set; }
    public string Unit { get; set; } = null!;
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public ICollection<GoalTarget> Targets { get; set; } = new List<GoalTarget>();
    public ICollection<CheckInMetric> Metrics { get; set; } = new List<CheckInMetric>();
}
