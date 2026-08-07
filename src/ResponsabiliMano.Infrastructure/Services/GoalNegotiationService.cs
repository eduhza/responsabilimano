using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Common;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class GoalNegotiationService : IGoalNegotiationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GoalNegotiationService> _logger;

    public GoalNegotiationService(AppDbContext context, ILogger<GoalNegotiationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GoalNegotiation> GetNegotiationAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Creator)
            .Include(p => p.Partner)
            .Include(p => p.Goals)
            .ThenInclude(g => g.Targets)
            .ThenInclude(t => t.User)
            .Include(p => p.Goals)
            .ThenInclude(g => g.Targets)
            .ThenInclude(t => t.Proposals)
            .ThenInclude(p => p.ProposedByUser)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
            throw new ArgumentException("Project not found.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        var acceptedCount = 0;
        var totalCount = 0;
        var items = new List<GoalNegotiationGoal>();

        foreach (var goal in project.Goals)
        {
            var creatorTarget = GetParticipantTarget(goal, project.CreatorId);
            var partnerTarget = project.PartnerId is null
                ? goal.Targets.FirstOrDefault(t => t.UserId is null)
                : GetParticipantTarget(goal, project.PartnerId.Value);

            var creatorView = MapTarget(creatorTarget, project.Creator, project.Partner);
            var partnerView = MapTarget(partnerTarget, project.Creator, project.Partner);

            if (creatorView is not null)
            {
                totalCount++;
                if (creatorView.Status == GoalTargetStatus.Accepted)
                    acceptedCount++;
            }

            if (partnerView is not null)
            {
                totalCount++;
                if (partnerView.Status == GoalTargetStatus.Accepted)
                    acceptedCount++;
            }

            items.Add(new GoalNegotiationGoal(
                goal.Id,
                goal.Label,
                goal.DataType,
                goal.Unit,
                goal.MinValue,
                goal.MaxValue,
                creatorView ?? new GoalNegotiationTarget(
                    Guid.Empty, null, null, null, GoalDirection.Reach,
                    GoalTargetStatus.PendingAcceptance, false, false, null, null, null, null),
                partnerView ?? new GoalNegotiationTarget(
                    Guid.Empty, null, null, null, GoalDirection.Reach,
                    GoalTargetStatus.PendingAcceptance, false, false, null, null, null, null)));
        }

        _logger.LogInformation(
            "Loaded negotiation for project {ProjectId} by user {UserId}: {AcceptedCount}/{TotalCount} accepted",
            projectId, userId, acceptedCount, totalCount);

        return new GoalNegotiation(acceptedCount, totalCount, project.CreatorId, project.PartnerId, items);
    }

    private static GoalTarget? GetParticipantTarget(GoalField goal, Guid userId) =>
        goal.Targets.FirstOrDefault(t => t.UserId == userId);

    private static GoalNegotiationTarget? MapTarget(
        GoalTarget? target,
        User? creator,
        User? partner)
    {
        if (target is null)
            return null;

        var lastProposal = target.Proposals
            .Where(p => p.Status != GoalProposalStatus.Superseded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        var lastProposerName = ResolveName(target.LastProposedByUserId, creator, partner);

        return new GoalNegotiationTarget(
            target.Id,
            target.UserId,
            target.Baseline,
            target.TargetValue,
            target.Direction,
            target.Status,
            target.AcceptedByCreator,
            target.AcceptedByPartner,
            target.LastProposedByUserId,
            target.LastProposedAt,
            lastProposerName,
            lastProposal?.Comment);
    }

    private static string? ResolveName(Guid? userId, User? creator, User? partner) => userId switch
    {
        _ when creator is not null && userId == creator.Id => creator.Name,
        _ when partner is not null && userId == partner.Id => partner.Name,
        _ => null
    };

    public async Task<GoalProposal> ProposeTargetAsync(
        Guid projectId,
        Guid goalTargetId,
        Guid userId,
        decimal? target,
        decimal? baseline,
        GoalDirection direction,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (comment is not null && comment.Length > 500)
            throw new ArgumentException("Comment cannot exceed 500 characters.");

        var goalTarget = await _context.GoalTargets
            .Include(t => t.GoalField)
            .ThenInclude(g => g.Project)
            .FirstOrDefaultAsync(t => t.Id == goalTargetId, cancellationToken);

        if (goalTarget is null)
            throw new ArgumentException("Goal target not found.");

        var project = goalTarget.GoalField.Project;

        if (project.Id != projectId)
            throw new ArgumentException("Goal target does not belong to the specified project.");

        if (project.CreatorId != userId && project.PartnerId != userId)
            throw new UnauthorizedAccessException("You are not a participant of this project.");

        if (project.Status is ProjectStatus.Finished or ProjectStatus.Cancelled)
            throw new InvalidOperationException("Cannot propose changes to a finished or cancelled project.");

        var dataType = goalTarget.GoalField.DataType;
        var minValue = goalTarget.GoalField.MinValue;
        var maxValue = goalTarget.GoalField.MaxValue;

        var normalizedBaseline = Normalize(dataType, baseline);
        var normalizedTarget = Normalize(dataType, target);

        if (GoalValueRules.ValidateTarget(dataType, minValue, maxValue, normalizedBaseline, normalizedTarget, direction) is { } error)
            throw new GoalValueException(error, dataType, goalTarget.GoalField.Label, minValue, maxValue, normalizedTarget, normalizedBaseline);

        var (acceptedByCreator, acceptedByPartner) = project.CreatorId == userId
            ? (true, false)
            : (false, true);

        var now = DateTime.UtcNow;

        goalTarget.Baseline = normalizedBaseline;
        goalTarget.TargetValue = normalizedTarget;
        goalTarget.Direction = direction;
        goalTarget.Status = GoalTargetStatus.PendingAcceptance;
        goalTarget.AcceptedByCreator = acceptedByCreator;
        goalTarget.AcceptedByPartner = acceptedByPartner;
        goalTarget.LastProposedByUserId = userId;
        goalTarget.LastProposedAt = now;

        var previousPending = await _context.GoalProposals
            .Where(p => p.GoalTargetId == goalTargetId && p.Status == GoalProposalStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousPending)
        {
            previous.Status = GoalProposalStatus.Superseded;
        }

        var proposal = new GoalProposal
        {
            Id = Guid.NewGuid(),
            GoalTargetId = goalTargetId,
            ProposedByUserId = userId,
            TargetValue = normalizedTarget,
            Baseline = normalizedBaseline,
            Direction = direction,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAt = now,
            Status = GoalProposalStatus.Pending
        };

        _context.GoalProposals.Add(proposal);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} proposed goal target {GoalTargetId} for project {ProjectId}: status {Status}, acceptedByCreator {AcceptedByCreator}, acceptedByPartner {AcceptedByPartner}",
            userId, goalTargetId, project.Id, goalTarget.Status, goalTarget.AcceptedByCreator, goalTarget.AcceptedByPartner);

        return proposal;
    }

    public async Task AcceptTargetAsync(
        Guid projectId,
        Guid goalTargetId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var goalTarget = await _context.GoalTargets
                .Include(t => t.GoalField)
                .ThenInclude(g => g.Project)
                .ThenInclude(p => p.Goals)
                .ThenInclude(g => g.Targets)
                .FirstOrDefaultAsync(t => t.Id == goalTargetId, cancellationToken);

            if (goalTarget is null)
                throw new ArgumentException("Goal target not found.");

            var project = goalTarget.GoalField.Project;

            if (project.Id != projectId)
                throw new ArgumentException("Goal target does not belong to the specified project.");

            if (project.CreatorId != userId && project.PartnerId != userId)
                throw new UnauthorizedAccessException("You are not a participant of this project.");

            if (goalTarget.LastProposedByUserId == userId)
                throw new UnauthorizedAccessException("You cannot accept your own proposal.");

            if (project.CreatorId == userId)
                goalTarget.AcceptedByCreator = true;
            else
                goalTarget.AcceptedByPartner = true;

            if (goalTarget.AcceptedByCreator && goalTarget.AcceptedByPartner)
            {
                goalTarget.Status = GoalTargetStatus.Accepted;

                var pendingProposal = await _context.GoalProposals
                    .Where(p => p.GoalTargetId == goalTargetId && p.Status == GoalProposalStatus.Pending)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (pendingProposal is not null)
                    pendingProposal.Status = GoalProposalStatus.Accepted;
            }

            if (project.PartnerId is not null && AllGoalTargetsAccepted(project))
            {
                project.Status = ProjectStatus.Active;
                _logger.LogInformation(
                    "Project {ProjectId} automatically activated after all goal targets were accepted",
                    project.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "User {UserId} accepted goal target {GoalTargetId} for project {ProjectId}: status {Status}, acceptedByCreator {AcceptedByCreator}, acceptedByPartner {AcceptedByPartner}",
                userId, goalTargetId, project.Id, goalTarget.Status, goalTarget.AcceptedByCreator, goalTarget.AcceptedByPartner);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> AcceptAllPendingAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var project = await _context.Projects
                .Include(p => p.Goals)
                .ThenInclude(g => g.Targets)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project is null)
                throw new ArgumentException("Project not found.");

            if (project.CreatorId != userId && project.PartnerId != userId)
                throw new UnauthorizedAccessException("You are not a participant of this project.");

            var acceptedCount = 0;
            var allTargets = project.Goals.SelectMany(g => g.Targets).ToList();

            foreach (var target in allTargets)
            {
                if (target.Status == GoalTargetStatus.Accepted)
                    continue;

                if (target.LastProposedByUserId == userId)
                    continue;

                var isCreator = project.CreatorId == userId;
                if (isCreator)
                {
                    if (target.AcceptedByCreator)
                        continue;
                    target.AcceptedByCreator = true;
                }
                else
                {
                    if (target.AcceptedByPartner)
                        continue;
                    target.AcceptedByPartner = true;
                }

                if (target.AcceptedByCreator && target.AcceptedByPartner)
                {
                    target.Status = GoalTargetStatus.Accepted;

                    var pendingProposal = await _context.GoalProposals
                        .Where(p => p.GoalTargetId == target.Id && p.Status == GoalProposalStatus.Pending)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (pendingProposal is not null)
                        pendingProposal.Status = GoalProposalStatus.Accepted;
                }

                acceptedCount++;
            }

            if (project.PartnerId is not null && AllGoalTargetsAccepted(project))
            {
                project.Status = ProjectStatus.Active;
                _logger.LogInformation(
                    "Project {ProjectId} automatically activated after all goal targets were accepted",
                    project.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "User {UserId} accepted {AcceptedCount} pending goal targets for project {ProjectId}",
                userId, acceptedCount, projectId);

            return acceptedCount;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool AllGoalTargetsAccepted(Project project) =>
        project.Goals.SelectMany(g => g.Targets).All(t => t.Status == GoalTargetStatus.Accepted);

    private static decimal? Normalize(GoalDataType dataType, decimal? value) =>
        value is { } present ? GoalValueRules.Normalize(dataType, present) : null;
}
