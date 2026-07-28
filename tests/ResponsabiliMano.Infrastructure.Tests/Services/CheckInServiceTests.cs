using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

/// <summary>Service tests for check-in capture (spec S3.2).</summary>
public class CheckInServiceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();

    public void Dispose() => _context.Dispose();

    private CheckInService CreateService() => new(_context, NullLogger<CheckInService>.Instance);

    private User SeedUser(string email = "creator@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "User",
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
        ProjectStatus status = ProjectStatus.Active,
        DateTime? startDate = null,
        decimal? min = null,
        decimal? max = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = startDate ?? DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = status
        };
        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = "Weight",
            DataType = GoalDataType.Decimal,
            Unit = "kg",
            MinValue = min,
            MaxValue = max
        };
        _context.Projects.Add(project);
        _context.GoalFields.Add(goal);
        _context.SaveChanges();
        return (project, goal);
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenProjectNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitCheckInAsync(
            Guid.NewGuid(), Guid.NewGuid(), Feeling.Happy, Array.Empty<CheckInMetricInput>()));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SubmitCheckInAsync(
            project.Id, Guid.NewGuid(), Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Theory]
    [InlineData(ProjectStatus.Pending)]
    [InlineData(ProjectStatus.Finished)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task SubmitCheckInAsync_Throws_WhenProjectNotActive(ProjectStatus status)
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, status: status);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenProjectNotStarted()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, startDate: DateTime.UtcNow.AddDays(5));
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenNoMetrics()
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, Array.Empty<CheckInMetricInput>()));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenGoalNotInProject()
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(Guid.NewGuid(), 1m) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenValueOutOfRange()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 0, max: 100);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 150m) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenDuplicateGoal()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy,
            new[] { new CheckInMetricInput(goal.Id, 1m), new CheckInMetricInput(goal.Id, 2m) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Persists_OnHappyPath()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 0, max: 100);
        var service = CreateService();

        var checkIn = await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.VeryHappy, new[] { new CheckInMetricInput(goal.Id, 80m) });

        Assert.Equal(1, checkIn.PeriodNumber);
        Assert.Equal(DateTimeKind.Utc, checkIn.SubmittedAt.Kind);
        var stored = await _context.CheckIns.Include(c => c.Metrics).SingleAsync();
        Assert.Equal(Feeling.VeryHappy, stored.Feeling);
        Assert.Equal(80m, Assert.Single(stored.Metrics).Value);
    }

    [Fact]
    public async Task SubmitCheckInAsync_Throws_WhenAlreadySubmittedThisPeriod()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 2m) }));
    }

    [Fact]
    public async Task GetCheckInFormAsync_ReturnsNull_WhenProjectMissing()
    {
        var service = CreateService();
        Assert.Null(await service.GetCheckInFormAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCheckInFormAsync_Throws_WhenNotParticipant()
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetCheckInFormAsync(project.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCheckInFormAsync_FlagsAlreadySubmitted()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();
        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        var form = await service.GetCheckInFormAsync(project.Id, creator.Id);

        Assert.NotNull(form);
        Assert.True(form!.AlreadySubmitted);
        Assert.Equal(1, form.PeriodNumber);
        Assert.Single(form.Project.Goals);
    }
}
