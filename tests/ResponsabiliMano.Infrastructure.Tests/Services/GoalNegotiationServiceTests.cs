using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

public sealed class GoalNegotiationServiceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();
    private readonly FakeEmailService _email = new();

    public void Dispose() => _context.Dispose();

    private ProjectService CreateProjectService()
    {
        var goalNegotiation = new GoalNegotiationService(_context, NullLogger<GoalNegotiationService>.Instance);
        return new ProjectService(_context, _email, goalNegotiation, NullLogger<ProjectService>.Instance);
    }

    private GoalNegotiationService CreateGoalNegotiationService()
        => new(_context, NullLogger<GoalNegotiationService>.Instance);

    private User SeedUser(string email, string name = "User")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = "hash",
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private static GoalFieldInput Goal(
        string label = "Steps",
        string unit = "count",
        decimal? targetValue = 50m,
        decimal? baseline = null,
        GoalDirection direction = GoalDirection.Reach)
        => new(label, GoalDataType.Integer, unit, 0, 100,
            new GoalTargetInput(baseline, targetValue, direction));

    private async Task<Project> SeedProjectAsync(
        Guid creatorId,
        Guid? partnerId = null,
        ProjectStatus status = ProjectStatus.Pending)
    {
        var service = CreateProjectService();

        var project = await service.CreateProjectAsync(
            creatorId,
            "Sample",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            ProjectFrequency.Weekly,
            new[] { Goal() });

        if (partnerId is not null)
        {
            project.PartnerId = partnerId;
            var partnerTarget = project.Goals.SelectMany(g => g.Targets).FirstOrDefault(t => t.UserId is null);
            if (partnerTarget is not null)
                partnerTarget.UserId = partnerId;
        }

        project.Status = status;
        await _context.SaveChangesAsync();

        return project;
    }

    [Fact]
    public async Task GetNegotiationAsync_ReturnsStateWithBothTargets()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = await SeedProjectAsync(creator.Id, partner.Id);

        var goal = project.Goals.Single();
        var targets = goal.Targets.ToList();
        targets[0].Status = GoalTargetStatus.Accepted;
        targets[0].AcceptedByCreator = true;
        targets[0].AcceptedByPartner = true;
        await _context.SaveChangesAsync();

        var service = CreateGoalNegotiationService();

        var negotiation = await service.GetNegotiationAsync(project.Id, creator.Id);

        Assert.Equal(2, negotiation.TotalCount);
        Assert.Equal(1, negotiation.AcceptedCount);
        Assert.Equal(creator.Id, negotiation.CreatorId);
        Assert.Equal(partner.Id, negotiation.PartnerId);
        Assert.Single(negotiation.Goals);
        Assert.Equal(goal.Label, negotiation.Goals[0].Label);
    }

    [Fact]
    public async Task ProposeTargetAsync_ResetsOtherSideAcceptance_AndCreatesProposal()
    {
        var creator = SeedUser("creator@example.com");
        var project = await SeedProjectAsync(creator.Id);

        var goal = project.Goals.Single();
        var target = goal.Targets.First(t => t.UserId == creator.Id);

        var service = CreateGoalNegotiationService();

        var proposal = await service.ProposeTargetAsync(
            project.Id, target.Id, creator.Id, 70m, 0m, GoalDirection.Reach, "Let's be more ambitious");

        Assert.NotEqual(Guid.Empty, proposal.Id);
        Assert.Equal(GoalProposalStatus.Pending, proposal.Status);
        Assert.Equal(70m, target.TargetValue);
        Assert.Equal(0m, target.Baseline);
        Assert.True(target.AcceptedByCreator);
        Assert.False(target.AcceptedByPartner);
        Assert.Equal(creator.Id, target.LastProposedByUserId);
    }

    [Fact]
    public async Task ProposeTargetAsync_SupersedesPreviousPendingProposal()
    {
        var creator = SeedUser("creator@example.com");
        var project = await SeedProjectAsync(creator.Id);

        var goal = project.Goals.Single();
        var target = goal.Targets.First(t => t.UserId == creator.Id);

        var previousProposal = new GoalProposal
        {
            Id = Guid.NewGuid(),
            GoalTargetId = target.Id,
            ProposedByUserId = creator.Id,
            TargetValue = 50m,
            Baseline = 0m,
            Direction = GoalDirection.Reach,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            Status = GoalProposalStatus.Pending
        };
        _context.GoalProposals.Add(previousProposal);
        await _context.SaveChangesAsync();

        var service = CreateGoalNegotiationService();

        await service.ProposeTargetAsync(
            project.Id, target.Id, creator.Id, 60m, 0m, GoalDirection.Reach, null);

        Assert.Equal(GoalProposalStatus.Superseded, previousProposal.Status);
        Assert.Single(_context.GoalProposals.Where(p => p.GoalTargetId == target.Id && p.Status == GoalProposalStatus.Pending).ToList());
    }

    [Fact]
    public async Task AcceptTargetAsync_AcceptsAndActivatesProject_WhenBothSidesAgree()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = await SeedProjectAsync(creator.Id, partner.Id);

        var targets = project.Goals.SelectMany(g => g.Targets).ToList();
        var creatorTarget = targets.First(t => t.UserId == creator.Id);
        var partnerTarget = targets.First(t => t.UserId == partner.Id);

        var service = CreateGoalNegotiationService();

        // Partner accepts the creator's target
        await service.AcceptTargetAsync(project.Id, creatorTarget.Id, partner.Id);

        // Partner proposes their own target, then the creator accepts it
        await service.ProposeTargetAsync(project.Id, partnerTarget.Id, partner.Id,
            partnerTarget.TargetValue, partnerTarget.Baseline, partnerTarget.Direction, null);
        await service.AcceptTargetAsync(project.Id, partnerTarget.Id, creator.Id);

        Assert.Equal(GoalTargetStatus.Accepted, creatorTarget.Status);
        Assert.Equal(GoalTargetStatus.Accepted, partnerTarget.Status);
        Assert.Equal(ProjectStatus.Active, project.Status);
    }

    [Fact]
    public async Task AcceptTargetAsync_Throws_WhenUserTriesToAcceptOwnProposal()
    {
        var creator = SeedUser("creator@example.com");
        var project = await SeedProjectAsync(creator.Id);

        var goal = project.Goals.Single();
        var target = goal.Targets.First(t => t.UserId == creator.Id);

        var service = CreateGoalNegotiationService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AcceptTargetAsync(project.Id, target.Id, creator.Id));
    }

    [Fact]
    public async Task AcceptAllPendingAsync_AcceptsAllPendingAndActivatesProject()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = await SeedProjectAsync(creator.Id, partner.Id);

        var service = CreateGoalNegotiationService();

        var acceptedCount = await service.AcceptAllPendingAsync(project.Id, partner.Id);

        Assert.Equal(2, acceptedCount);
        Assert.True(project.Goals.SelectMany(g => g.Targets).All(t => t.Status == GoalTargetStatus.Accepted));
        Assert.Equal(ProjectStatus.Active, project.Status);
    }
}
