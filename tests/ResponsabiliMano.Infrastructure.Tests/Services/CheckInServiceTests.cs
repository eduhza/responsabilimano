using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Common;
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
        DateTime? endDate = null,
        decimal? min = null,
        decimal? max = null,
        GoalDataType dataType = GoalDataType.Decimal)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = startDate ?? DateTime.UtcNow,
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = status
        };
        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = dataType == GoalDataType.Boolean ? "Yes/No" : "Weight",
            DataType = dataType,
            Unit = dataType == GoalDataType.Boolean ? "" : "kg",
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

        await Assert.ThrowsAsync<GoalValueException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 150m) }));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(2)]
    public async Task SubmitCheckInAsync_Throws_WhenBooleanValueNot0Or1(decimal value)
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 0, max: 1, dataType: GoalDataType.Boolean);
        var service = CreateService();

        await Assert.ThrowsAsync<GoalValueException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, value) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Persists_BooleanValue()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 0, max: 1, dataType: GoalDataType.Boolean);
        var service = CreateService();

        var checkIn = await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        var stored = await _context.CheckIns.Include(c => c.Metrics).SingleAsync();
        Assert.Equal(1m, Assert.Single(stored.Metrics).Value);
    }

    [Theory]
    [InlineData(3.5)]
    [InlineData(6)]
    [InlineData(0)]
    public async Task SubmitCheckInAsync_Throws_WhenScaleValueInvalid(decimal value)
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 1, max: 5, dataType: GoalDataType.Scale);
        var service = CreateService();

        await Assert.ThrowsAsync<GoalValueException>(() => service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, value) }));
    }

    [Fact]
    public async Task SubmitCheckInAsync_Persists_ScaleValue()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 1, max: 5, dataType: GoalDataType.Scale);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 4m) });

        var stored = await _context.CheckIns.Include(c => c.Metrics).SingleAsync();
        Assert.Equal(4m, Assert.Single(stored.Metrics).Value);
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

    [Fact]
    public async Task GetCheckInFormsForUserAsync_ReturnsOnlyActiveStartedInWindowProjects()
    {
        var creator = SeedUser();
        var now = DateTime.UtcNow;

        var activeInWindow = SeedProject(creator.Id, startDate: now.AddDays(-7), endDate: now.AddDays(21));
        var pending = SeedProject(creator.Id, status: ProjectStatus.Pending);
        var notStarted = SeedProject(creator.Id, startDate: now.AddDays(7));
        var ended = SeedProject(creator.Id, startDate: now.AddDays(-60), endDate: now.AddDays(-1));

        var service = CreateService();
        var forms = await service.GetCheckInFormsForUserAsync(creator.Id);

        Assert.Single(forms);
        Assert.Equal(activeInWindow.project.Id, forms[0].Project.Id);
    }

    [Fact]
    public async Task GetCheckInFormsForUserAsync_FlagsAlreadySubmittedAndReturnsPeriodEnd()
    {
        var creator = SeedUser();
        var now = DateTime.UtcNow;
        var (project, goal) = SeedProject(creator.Id, startDate: now.AddDays(-7), endDate: now.AddDays(21));

        var service = CreateService();
        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        var forms = await service.GetCheckInFormsForUserAsync(creator.Id);

        Assert.Single(forms);
        Assert.True(forms[0].AlreadySubmitted);
        Assert.True(forms[0].PeriodNumber > 0);
        Assert.Single(forms[0].Project.Goals);
        Assert.True(forms[0].PeriodEnd > now);
        Assert.True(forms[0].PeriodEnd <= project.EndDate.Date.AddDays(1).AddTicks(-1));
    }

    [Fact]
    public async Task GetCheckInFormsForUserAsync_IncludesProjectsWhereUserIsPartner()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var now = DateTime.UtcNow;
        var (project, _) = SeedProject(creator.Id, partnerId: partner.Id, startDate: now.AddDays(-7), endDate: now.AddDays(21));

        var service = CreateService();
        var forms = await service.GetCheckInFormsForUserAsync(partner.Id);

        Assert.Single(forms);
        Assert.Equal(project.Id, forms[0].Project.Id);
    }

    [Fact]
    public async Task GetCheckInFormsForUserAsync_ReturnsEmpty_WhenUserHasNoProjects()
    {
        var user = SeedUser();
        var service = CreateService();

        var forms = await service.GetCheckInFormsForUserAsync(user.Id);

        Assert.Empty(forms);
    }

    [Fact]
    public async Task GetCheckInFormAsync_IncludesPeriodEnd()
    {
        var creator = SeedUser();
        var now = DateTime.UtcNow;
        var (project, _) = SeedProject(creator.Id, startDate: now.AddDays(-7), endDate: now.AddDays(21));

        var service = CreateService();
        var form = await service.GetCheckInFormAsync(project.Id, creator.Id);

        Assert.NotNull(form);
        Assert.True(form!.PeriodEnd > now);
        Assert.True(form.PeriodEnd <= project.EndDate.Date.AddDays(1).AddTicks(-1));
    }

    [Fact]
    public async Task GetCheckInFormAsync_ReturnsExistingCheckIn()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 80m) });

        var form = await service.GetCheckInFormAsync(project.Id, creator.Id);

        Assert.NotNull(form);
        Assert.True(form!.AlreadySubmitted);
        Assert.NotNull(form.Existing);
        Assert.Equal(Feeling.Happy, form.Existing!.Feeling);
        Assert.Single(form.Existing.Metrics);
    }

    [Fact]
    public async Task UpdateCurrentCheckInAsync_ReturnsNull_WhenProjectNotFound()
    {
        var service = CreateService();
        var result = await service.UpdateCurrentCheckInAsync(
            Guid.NewGuid(), Guid.NewGuid(), Feeling.Happy, []);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCurrentCheckInAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateCurrentCheckInAsync(
            project.Id, Guid.NewGuid(), Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Theory]
    [InlineData(ProjectStatus.Pending)]
    [InlineData(ProjectStatus.Finished)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task UpdateCurrentCheckInAsync_Throws_WhenProjectNotActive(ProjectStatus status)
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, status: status);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateCurrentCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Fact]
    public async Task UpdateCurrentCheckInAsync_Throws_WhenNoCurrentCheckIn()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateCurrentCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) }));
    }

    [Fact]
    public async Task UpdateCurrentCheckInAsync_Throws_WhenCheckInBelongsToAnotherUser()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var (project, goal) = SeedProject(creator.Id, partnerId: partner.Id);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateCurrentCheckInAsync(
            project.Id, partner.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 2m) }));
    }

    [Fact]
    public async Task UpdateCurrentCheckInAsync_Persists_CorrectedValues()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id, min: 0, max: 100);
        var service = CreateService();

        var checkIn = await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 80m) });

        var updated = await service.UpdateCurrentCheckInAsync(
            project.Id, creator.Id, Feeling.Neutral, new[] { new CheckInMetricInput(goal.Id, 95m) });

        Assert.NotNull(updated);
        Assert.Equal(checkIn.Id, updated!.Id);
        Assert.Equal(Feeling.Neutral, updated.Feeling);
        Assert.NotNull(updated.UpdatedAt);

        var stored = await _context.CheckIns.Include(c => c.Metrics).SingleAsync();
        Assert.Equal(checkIn.Id, stored.Id);
        Assert.Equal(Feeling.Neutral, stored.Feeling);
        Assert.Equal(95m, Assert.Single(stored.Metrics).Value);
    }

    [Fact]
    public async Task DeleteCurrentCheckInAsync_ReturnsFalse_WhenProjectNotFound()
    {
        var service = CreateService();
        var result = await service.DeleteCurrentCheckInAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCurrentCheckInAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCurrentCheckInAsync(
            project.Id, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(ProjectStatus.Pending)]
    public async Task DeleteCurrentCheckInAsync_Throws_WhenProjectNotActive(ProjectStatus status)
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id, status: status);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteCurrentCheckInAsync(
            project.Id, creator.Id));
    }

    [Fact]
    public async Task DeleteCurrentCheckInAsync_Throws_WhenNoCurrentCheckIn()
    {
        var creator = SeedUser();
        var (project, _) = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteCurrentCheckInAsync(
            project.Id, creator.Id));
    }

    [Fact]
    public async Task DeleteCurrentCheckInAsync_Throws_WhenCheckInBelongsToAnotherUser()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var (project, goal) = SeedProject(creator.Id, partnerId: partner.Id);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCurrentCheckInAsync(
            project.Id, partner.Id));
    }

    [Fact]
    public async Task DeleteCurrentCheckInAsync_RemovesCheckInAndMetrics()
    {
        var creator = SeedUser();
        var (project, goal) = SeedProject(creator.Id);
        var service = CreateService();

        await service.SubmitCheckInAsync(
            project.Id, creator.Id, Feeling.Happy, new[] { new CheckInMetricInput(goal.Id, 1m) });

        var deleted = await service.DeleteCurrentCheckInAsync(project.Id, creator.Id);

        Assert.True(deleted);
        Assert.Equal(0, await _context.CheckIns.CountAsync());
        Assert.Equal(0, await _context.CheckInMetrics.CountAsync());
    }
}
