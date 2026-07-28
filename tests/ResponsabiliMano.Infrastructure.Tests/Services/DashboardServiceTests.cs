using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

/// <summary>Service tests for the dashboard data API (spec S4.1).</summary>
public class DashboardServiceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();

    public void Dispose() => _context.Dispose();

    private DashboardService CreateService() => new(_context, NullLogger<DashboardService>.Instance);

    private User SeedUser(string name = "User", string email = "user@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private (Project project, GoalField goal) SeedProject(
        Guid creatorId,
        Guid? partnerId = null,
        string name = "Sample",
        decimal? targetValue = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active
        };
        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = "Weight",
            DataType = GoalDataType.Decimal,
            Unit = "kg",
            TargetValue = targetValue
        };
        _context.Projects.Add(project);
        _context.GoalFields.Add(goal);
        _context.SaveChanges();
        return (project, goal);
    }

    private void SeedCheckIn(
        Guid projectId,
        Guid userId,
        int periodNumber,
        Feeling feeling,
        params (Guid goalFieldId, decimal value)[] metrics)
    {
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = feeling,
            PeriodNumber = periodNumber,
            SubmittedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Metrics = metrics.Select(m => new CheckInMetric
            {
                Id = Guid.NewGuid(),
                GoalFieldId = m.goalFieldId,
                Value = m.value
            }).ToList()
        };
        _context.CheckIns.Add(checkIn);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNull_WhenProjectNotFound()
    {
        var service = CreateService();
        var result = await service.GetDashboardAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDashboardAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser("Creator", "creator@example.com");
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetDashboardAsync(project.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDashboardAsync_HappyPath_TwoUsersMultipleCheckIns()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var partner = SeedUser("Bob", "bob@example.com");
        var (project, goal) = SeedProject(creator.Id, partner.Id, "Diet Project", targetValue: 70m);

        SeedCheckIn(project.Id, creator.Id, 1, Feeling.Happy, (goal.Id, 80m));
        SeedCheckIn(project.Id, partner.Id, 1, Feeling.Neutral, (goal.Id, 75m));
        SeedCheckIn(project.Id, creator.Id, 2, Feeling.VeryHappy, (goal.Id, 78m));
        SeedCheckIn(project.Id, partner.Id, 2, Feeling.Sad, (goal.Id, 73m));

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result!.ProjectId);
        Assert.Equal("Diet Project", result.ProjectName);

        Assert.Equal(2, result.Participants.Count);
        var alice = result.Participants.Single(p => p.UserId == creator.Id);
        var bob = result.Participants.Single(p => p.UserId == partner.Id);
        Assert.Equal("Alice", alice.Name);
        Assert.Equal(Feeling.VeryHappy, alice.LatestFeeling);
        Assert.Equal("Bob", bob.Name);
        Assert.Equal(Feeling.Sad, bob.LatestFeeling);

        Assert.Single(result.Metrics);
        var series = result.Metrics[0];
        Assert.Equal(goal.Id, series.GoalFieldId);
        Assert.Equal("Weight", series.Label);
        Assert.Equal("kg", series.Unit);
        Assert.Equal(GoalDataType.Decimal, series.DataType);
        Assert.Equal(70m, series.TargetValue);

        Assert.Equal(4, series.Series.Count);

        var ordered = series.Series.OrderBy(e => e.PeriodNumber).ThenBy(e => e.UserId).ToList();
        Assert.Equal(2, ordered.Count(e => e.PeriodNumber == 1));
        Assert.Equal(2, ordered.Count(e => e.PeriodNumber == 2));

        var creatorPeriod1 = series.Series.Single(e => e.UserId == creator.Id && e.PeriodNumber == 1);
        Assert.Equal(80m, creatorPeriod1.Value);
        Assert.Equal(79m, creatorPeriod1.AverageValue);

        var partnerPeriod1 = series.Series.Single(e => e.UserId == partner.Id && e.PeriodNumber == 1);
        Assert.Equal(75m, partnerPeriod1.Value);
        Assert.Equal(74m, partnerPeriod1.AverageValue);

        var creatorPeriod2 = series.Series.Single(e => e.UserId == creator.Id && e.PeriodNumber == 2);
        Assert.Equal(78m, creatorPeriod2.Value);
        Assert.Equal(79m, creatorPeriod2.AverageValue);

        var partnerPeriod2 = series.Series.Single(e => e.UserId == partner.Id && e.PeriodNumber == 2);
        Assert.Equal(73m, partnerPeriod2.Value);
        Assert.Equal(74m, partnerPeriod2.AverageValue);
    }

    [Fact]
    public async Task GetDashboardAsync_NoCheckIns_LatestFeelingNull_AverageNull_SeriesEmpty()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var partner = SeedUser("Bob", "bob@example.com");
        var (project, goal) = SeedProject(creator.Id, partner.Id);

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Participants.Count);
        Assert.All(result.Participants, p => Assert.Null(p.LatestFeeling));

        Assert.Single(result.Metrics);
        Assert.Empty(result.Metrics[0].Series);
        Assert.Null(result.Metrics[0].TargetValue);
    }

    [Fact]
    public async Task GetDashboardAsync_SeriesOrderedByPeriodNumberAscending()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var (project, goal) = SeedProject(creator.Id);

        SeedCheckIn(project.Id, creator.Id, 3, Feeling.Happy, (goal.Id, 70m));
        SeedCheckIn(project.Id, creator.Id, 1, Feeling.Sad, (goal.Id, 80m));
        SeedCheckIn(project.Id, creator.Id, 2, Feeling.Neutral, (goal.Id, 75m));

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        var series = result!.Metrics[0].Series;
        Assert.Equal(3, series.Count);
        Assert.Equal([1, 2, 3], series.Select(e => e.PeriodNumber).ToArray());
    }

    [Fact]
    public async Task GetDashboardAsync_SingleParticipant_NoPartner()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var (project, goal) = SeedProject(creator.Id, partnerId: null);

        SeedCheckIn(project.Id, creator.Id, 1, Feeling.Happy, (goal.Id, 80m));

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        Assert.Single(result!.Participants);
        Assert.Equal(creator.Id, result.Participants[0].UserId);
        Assert.Equal(Feeling.Happy, result.Participants[0].LatestFeeling);
    }

    [Fact]
    public async Task GetDashboardAsync_AverageCalculatedOverAllCheckInsForGoalField()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var (project, goal) = SeedProject(creator.Id);

        SeedCheckIn(project.Id, creator.Id, 1, Feeling.Happy, (goal.Id, 80m));
        SeedCheckIn(project.Id, creator.Id, 2, Feeling.Happy, (goal.Id, 70m));
        SeedCheckIn(project.Id, creator.Id, 3, Feeling.Happy, (goal.Id, 60m));

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        var series = result!.Metrics[0].Series;
        Assert.Equal(3, series.Count);
        Assert.All(series, e => Assert.Equal(70m, e.AverageValue));
    }

    [Fact]
    public async Task GetDashboardAsync_MultipleGoals_EachHasOwnSeries()
    {
        var creator = SeedUser("Alice", "alice@example.com");
        var partner = SeedUser("Bob", "bob@example.com");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Multi Goal",
            CreatorId = creator.Id,
            PartnerId = partner.Id,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active
        };
        var goal1 = new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = "Weight",
            DataType = GoalDataType.Decimal,
            Unit = "kg"
        };
        var goal2 = new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = "Sleep",
            DataType = GoalDataType.Integer,
            Unit = "h"
        };
        _context.Projects.Add(project);
        _context.GoalFields.AddRange(goal1, goal2);
        _context.SaveChanges();

        SeedCheckIn(project.Id, creator.Id, 1, Feeling.Happy, (goal1.Id, 80m), (goal2.Id, 7m));
        SeedCheckIn(project.Id, partner.Id, 1, Feeling.Neutral, (goal1.Id, 75m), (goal2.Id, 6m));

        var service = CreateService();
        var result = await service.GetDashboardAsync(project.Id, creator.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Metrics.Count);

        var weightSeries = result.Metrics.Single(m => m.Label == "Weight");
        var sleepSeries = result.Metrics.Single(m => m.Label == "Sleep");
        Assert.Equal(2, weightSeries.Series.Count);
        Assert.Equal(2, sleepSeries.Series.Count);
        Assert.Equal("h", sleepSeries.Unit);
        Assert.Equal(GoalDataType.Integer, sleepSeries.DataType);
    }
}
