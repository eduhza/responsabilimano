using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Tests.WebTestFakes;

public sealed class FakeGoalNegotiationService : IGoalNegotiationService
{
    public GoalNegotiation? Negotiation { get; set; }
    public int AcceptAllCount { get; set; }

    public Task<GoalNegotiation> GetNegotiationAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (Negotiation is null)
        {
            return Task.FromResult(new GoalNegotiation(0, 0, Guid.Empty, null, []));
        }

        return Task.FromResult(Negotiation);
    }

    public Task<GoalProposal> ProposeTargetAsync(
        Guid projectId,
        Guid goalTargetId,
        Guid userId,
        decimal? target,
        decimal? baseline,
        GoalDirection direction,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GoalProposal
        {
            Id = Guid.NewGuid(),
            GoalTargetId = goalTargetId,
            ProposedByUserId = userId,
            TargetValue = target,
            Baseline = baseline,
            Direction = direction,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
            Status = GoalProposalStatus.Pending
        });
    }

    public Task AcceptTargetAsync(Guid projectId, Guid goalTargetId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<int> AcceptAllPendingAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AcceptAllCount);
    }
}
