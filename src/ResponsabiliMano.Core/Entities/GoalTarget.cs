using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class GoalTarget
{
    public Guid Id { get; set; }
    public Guid GoalFieldId { get; set; }
    public Guid? UserId { get; set; }
    public decimal? Baseline { get; set; }
    public decimal? TargetValue { get; set; }
    public GoalDirection Direction { get; set; }
    public GoalTargetStatus Status { get; set; }
    public bool AcceptedByCreator { get; set; }
    public bool AcceptedByPartner { get; set; }
    public Guid? LastProposedByUserId { get; set; }
    public DateTime? LastProposedAt { get; set; }

    public GoalField GoalField { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<GoalProposal> Proposals { get; set; } = new List<GoalProposal>();
}
