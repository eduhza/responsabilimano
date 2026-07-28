using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

/// <summary>Service tests for the scheduler-driven jobs (specs S3.3 and S3.4).</summary>
public class CheckInNotificationServiceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();
    private readonly FakeEmailService _email = new();
    private static readonly DateTime Now = new(2026, 3, 6, 8, 0, 0, DateTimeKind.Utc);

    public void Dispose() => _context.Dispose();

    private CheckInNotificationService CreateService()
        => new(_context, _email, NullLogger<CheckInNotificationService>.Instance);

    private User SeedUser(string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = email.Split('@')[0],
            Email = email,
            PasswordHash = "hash",
            CreatedAt = Now
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    private Project SeedProject(
        Guid creatorId,
        Guid? partnerId,
        ProjectStatus status = ProjectStatus.Active,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = startDate ?? Now,
            EndDate = endDate ?? Now.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = status
        };
        _context.Projects.Add(project);
        _context.SaveChanges();
        return project;
    }

    private void SeedCheckIn(Guid projectId, Guid userId, int period)
    {
        _context.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Feeling = Feeling.Neutral,
            PeriodNumber = period,
            SubmittedAt = Now
        });
        _context.SaveChanges();
    }

    // ---------- DispatchCheckInEmailsAsync (S3.3) ----------

    [Fact]
    public async Task Dispatch_SendsToBothParticipants()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        var sent = await service.DispatchCheckInEmailsAsync(Now, "https://app");

        Assert.Equal(2, sent);
        Assert.Equal(2, _email.SentEmails.Count);
        Assert.Equal(2, await _context.CheckInNotifications.CountAsync());
    }

    [Fact]
    public async Task Dispatch_IsIdempotent_WithinSamePeriod()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        await service.DispatchCheckInEmailsAsync(Now, "https://app");
        var secondRun = await service.DispatchCheckInEmailsAsync(Now, "https://app");

        Assert.Equal(0, secondRun);
        Assert.Equal(2, _email.SentEmails.Count);
    }

    [Fact]
    public async Task Dispatch_SkipsInactiveAndEndedProjects()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id, status: ProjectStatus.Pending);
        SeedProject(creator.Id, partner.Id, endDate: Now.AddDays(-1)); // already ended
        var service = CreateService();

        var sent = await service.DispatchCheckInEmailsAsync(Now, "https://app");

        Assert.Equal(0, sent);
        Assert.Empty(_email.SentEmails);
    }

    [Fact]
    public async Task Dispatch_SkipsProjectNotStarted()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id, startDate: Now.AddDays(3));
        var service = CreateService();

        var sent = await service.DispatchCheckInEmailsAsync(Now, "https://app");

        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task Dispatch_SendsToSoloCreator_WhenNoPartner()
    {
        var creator = SeedUser("creator@example.com");
        SeedProject(creator.Id, partnerId: null);
        var service = CreateService();

        var sent = await service.DispatchCheckInEmailsAsync(Now, "https://app");

        Assert.Equal(1, sent);
        Assert.Equal("creator@example.com", Assert.Single(_email.SentEmails).To);
    }

    // ---------- DispatchRemindersAsync (S3.4) ----------

    [Fact]
    public async Task Reminders_SkipsParticipantsWhoSubmitted()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id);
        SeedCheckIn(project.Id, creator.Id, period: 1); // creator already did it
        var service = CreateService();

        var sent = await service.DispatchRemindersAsync(Now, "https://app");

        Assert.Equal(1, sent);
        Assert.Equal("partner@example.com", Assert.Single(_email.SentEmails).To);
    }

    [Fact]
    public async Task Reminders_SendToAll_WhenNobodySubmitted()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        var sent = await service.DispatchRemindersAsync(Now, "https://app");

        Assert.Equal(2, sent);
    }

    [Fact]
    public async Task Reminders_AreIdempotent_WithinSamePeriod()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        await service.DispatchRemindersAsync(Now, "https://app");
        var secondRun = await service.DispatchRemindersAsync(Now, "https://app");

        Assert.Equal(0, secondRun);
        Assert.Equal(2, _email.SentEmails.Count);
    }

    [Fact]
    public async Task Reminders_SkipInactiveProjects()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        SeedProject(creator.Id, partner.Id, status: ProjectStatus.Finished);
        var service = CreateService();

        var sent = await service.DispatchRemindersAsync(Now, "https://app");

        Assert.Equal(0, sent);
    }
}
