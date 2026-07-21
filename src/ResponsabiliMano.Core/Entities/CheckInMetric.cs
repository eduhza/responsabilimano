namespace ResponsabiliMano.Core.Entities;

public class CheckInMetric
{
    public Guid Id { get; set; }
    public Guid CheckInId { get; set; }
    public Guid GoalFieldId { get; set; }
    public decimal Value { get; set; }

    public CheckIn CheckIn { get; set; } = null!;
    public GoalField GoalField { get; set; } = null!;
}
