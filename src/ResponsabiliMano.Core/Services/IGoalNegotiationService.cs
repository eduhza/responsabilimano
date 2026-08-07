using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Services;

public interface IGoalNegotiationService
{
    Task<GoalNegotiation> GetNegotiationAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<GoalProposal> ProposeTargetAsync(
        Guid projectId,
        Guid goalTargetId,
        Guid userId,
        decimal? target,
        decimal? baseline,
        GoalDirection direction,
        string? comment,
        CancellationToken cancellationToken = default);

    Task AcceptTargetAsync(
        Guid projectId,
        Guid goalTargetId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> AcceptAllPendingAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record GoalNegotiation(
    int AcceptedCount,
    int TotalCount,
    Guid CreatorId,
    Guid? PartnerId,
    IReadOnlyList<GoalNegotiationGoal> Goals);

public sealed record GoalNegotiationGoal(
    Guid GoalFieldId,
    string Label,
    GoalDataType DataType,
    string Unit,
    decimal? MinValue,
    decimal? MaxValue,
    GoalNegotiationTarget CreatorTarget,
    GoalNegotiationTarget PartnerTarget);

public sealed record GoalNegotiationTarget(
    Guid GoalTargetId,
    Guid? UserId,
    decimal? Baseline,
    decimal? TargetValue,
    GoalDirection Direction,
    GoalTargetStatus Status,
    bool AcceptedByCreator,
    bool AcceptedByPartner,
    Guid? LastProposedByUserId,
    DateTime? LastProposedAt,
    string? LastProposerName,
    string? LastComment);
