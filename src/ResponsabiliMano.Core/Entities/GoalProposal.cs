using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class GoalProposal
{
    public Guid Id { get; set; }
    public Guid GoalTargetId { get; set; }
    public Guid ProposedByUserId { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? Baseline { get; set; }
    public GoalDirection Direction { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public GoalProposalStatus Status { get; set; }

    public GoalTarget GoalTarget { get; set; } = null!;
    public User ProposedByUser { get; set; } = null!;
}
