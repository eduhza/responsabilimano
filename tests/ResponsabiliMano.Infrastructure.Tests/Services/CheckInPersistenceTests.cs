using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

/// <summary>
/// Data-model tests for the check-in tables (spec S3.1): the unique constraint per
/// (project, user, period) and cascade delete of metrics. These run against SQLite,
/// which honours relational constraints faithfully.
/// </summary>
public class CheckInPersistenceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();

    public void Dispose() => _context.Dispose();

    private (User user, Project project, GoalField goal) SeedActiveProject()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Email = "user@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            CreatorId = user.Id,
            StartDate = DateTime.UtcNow,
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
            Unit = "kg"
        };
        _context.Users.Add(user);
        _context.Projects.Add(project);
        _context.GoalFields.Add(goal);
        _context.SaveChanges();
        return (user, project, goal);
    }

    private CheckIn NewCheckIn(Guid projectId, Guid userId, int period, params CheckInMetric[] metrics)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = Feeling.Happy,
            PeriodNumber = period,
            SubmittedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Metrics = metrics.ToList()
        };

    [Fact]
    public async Task CheckIn_PersistsWithMetrics()
    {
        var (user, project, goal) = SeedActiveProject();
        var checkIn = NewCheckIn(project.Id, user.Id, 1,
            new CheckInMetric { Id = Guid.NewGuid(), GoalFieldId = goal.Id, Value = 72.5m });

        _context.CheckIns.Add(checkIn);
        await _context.SaveChangesAsync();

        var stored = await _context.CheckIns.Include(c => c.Metrics).SingleAsync();
        Assert.Equal(1, stored.PeriodNumber);
        var metric = Assert.Single(stored.Metrics);
        Assert.Equal(72.5m, metric.Value);
    }

    [Fact]
    public async Task CheckIn_UniqueConstraint_RejectsDuplicatePeriodPerUser()
    {
        var (user, project, _) = SeedActiveProject();
        _context.CheckIns.Add(NewCheckIn(project.Id, user.Id, 1));
        await _context.SaveChangesAsync();

        _context.CheckIns.Add(NewCheckIn(project.Id, user.Id, 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task CheckIn_AllowsSameUser_InDifferentPeriods()
    {
        var (user, project, _) = SeedActiveProject();
        _context.CheckIns.Add(NewCheckIn(project.Id, user.Id, 1));
        _context.CheckIns.Add(NewCheckIn(project.Id, user.Id, 2));

        await _context.SaveChangesAsync();

        Assert.Equal(2, await _context.CheckIns.CountAsync());
    }

    [Fact]
    public async Task DeletingCheckIn_CascadeDeletesMetrics()
    {
        var (user, project, goal) = SeedActiveProject();
        var checkIn = NewCheckIn(project.Id, user.Id, 1,
            new CheckInMetric { Id = Guid.NewGuid(), GoalFieldId = goal.Id, Value = 10m });
        _context.CheckIns.Add(checkIn);
        await _context.SaveChangesAsync();

        _context.CheckIns.Remove(checkIn);
        await _context.SaveChangesAsync();

        Assert.Equal(0, await _context.CheckInMetrics.CountAsync());
    }
}
