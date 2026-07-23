using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

public class ProjectServiceTests : IDisposable
{
    private readonly AppDbContext _context = TestDbContextFactory.Create();
    private readonly FakeEmailService _email = new();

    public void Dispose() => _context.Dispose();

    private ProjectService CreateService()
        => new(_context, _email, NullLogger<ProjectService>.Instance);

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

    private Project SeedProject(
        Guid creatorId,
        Guid? partnerId = null,
        ProjectStatus status = ProjectStatus.Pending)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sample",
            CreatorId = creatorId,
            PartnerId = partnerId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Frequency = ProjectFrequency.Weekly,
            Status = status
        };
        _context.Projects.Add(project);
        _context.SaveChanges();
        return project;
    }

    private static GoalFieldInput Goal(string label = "Steps", string unit = "count")
        => new(label, GoalDataType.Integer, unit, null, null, null);

    // ---------- CreateProjectAsync ----------

    [Fact]
    public async Task CreateProjectAsync_Throws_WhenEndDateNotAfterStartDate()
    {
        var service = CreateService();
        var start = DateTime.UtcNow;

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateProjectAsync(
            Guid.NewGuid(), "P", start, start, ProjectFrequency.Daily, new[] { Goal() }));
    }

    [Fact]
    public async Task CreateProjectAsync_PersistsProject_WithTrimmedNameGoalsAndPendingStatus()
    {
        var service = CreateService();
        var creatorId = SeedUser("creator@example.com").Id;

        var project = await service.CreateProjectAsync(
            creatorId,
            "  My Project  ",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(10),
            ProjectFrequency.Weekly,
            new[] { new GoalFieldInput("  Steps  ", GoalDataType.Integer, "  count  ", 0, 100, 50) });

        Assert.Equal("My Project", project.Name);
        Assert.Equal(ProjectStatus.Pending, project.Status);
        Assert.Equal(creatorId, project.CreatorId);
        var goal = Assert.Single(project.Goals);
        Assert.Equal("Steps", goal.Label);
        Assert.Equal("count", goal.Unit);
        Assert.Single(_context.Projects);
    }

    [Fact]
    public async Task CreateProjectAsync_Throws_WhenGoalLabelBlank()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateProjectAsync(
            Guid.NewGuid(), "P", DateTime.UtcNow, DateTime.UtcNow.AddDays(5),
            ProjectFrequency.Daily, new[] { new GoalFieldInput("  ", GoalDataType.Integer, "count", null, null, null) }));
    }

    [Fact]
    public async Task CreateProjectAsync_Throws_WhenGoalUnitBlank()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateProjectAsync(
            Guid.NewGuid(), "P", DateTime.UtcNow, DateTime.UtcNow.AddDays(5),
            ProjectFrequency.Daily, new[] { new GoalFieldInput("Label", GoalDataType.Integer, " ", null, null, null) }));
    }

    // ---------- InvitePartnerAsync ----------

    [Fact]
    public async Task InvitePartnerAsync_Throws_WhenProjectNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.InvitePartnerAsync(
            Guid.NewGuid(), Guid.NewGuid(), "p@example.com", "https://app"));
    }

    [Fact]
    public async Task InvitePartnerAsync_Throws_WhenInviterIsNotCreator()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.InvitePartnerAsync(
            project.Id, Guid.NewGuid(), "p@example.com", "https://app"));
    }

    [Fact]
    public async Task InvitePartnerAsync_Throws_WhenInvitingSelf()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.InvitePartnerAsync(
            project.Id, creator.Id, "Creator@Example.com", "https://app"));
    }

    [Fact]
    public async Task InvitePartnerAsync_CreatesInvitationAndSendsEmail()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        var invitation = await service.InvitePartnerAsync(
            project.Id, creator.Id, "  Partner@Example.com ", "https://app/");

        Assert.Equal("partner@example.com", invitation.Email);
        Assert.False(string.IsNullOrWhiteSpace(invitation.Token));
        Assert.True(invitation.ExpiresAt > DateTime.UtcNow);
        Assert.Single(_context.ProjectInvitations);

        var sent = Assert.Single(_email.SentEmails);
        Assert.Equal("partner@example.com", sent.To);
        Assert.Contains(invitation.Token, sent.HtmlBody);
    }

    // ---------- AcceptInvitationAsync ----------

    private ProjectInvitation SeedInvitation(Guid projectId, string email, DateTime? expiresAt = null, DateTime? acceptedAt = null)
    {
        var invitation = new ProjectInvitation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Email = email,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            AcceptedAt = acceptedAt
        };
        _context.ProjectInvitations.Add(invitation);
        _context.SaveChanges();
        return invitation;
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNull_ForUnknownToken()
    {
        var service = CreateService();

        Assert.Null(await service.AcceptInvitationAsync("nope", Guid.NewGuid()));
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNull_WhenExpired()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, partner.Email, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var service = CreateService();

        Assert.Null(await service.AcceptInvitationAsync(invitation.Token, partner.Id));
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNull_WhenAlreadyAccepted()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, partner.Email, acceptedAt: DateTime.UtcNow.AddMinutes(-10));
        var service = CreateService();

        Assert.Null(await service.AcceptInvitationAsync(invitation.Token, partner.Id));
    }

    [Fact]
    public async Task AcceptInvitationAsync_ReturnsNull_WhenUserEmailDoesNotMatch()
    {
        var creator = SeedUser("creator@example.com");
        var other = SeedUser("other@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, "partner@example.com");
        var service = CreateService();

        Assert.Null(await service.AcceptInvitationAsync(invitation.Token, other.Id));
    }

    [Fact]
    public async Task AcceptInvitationAsync_SetsPartnerAndAcceptedAt_OnSuccess()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, "Partner@Example.com");
        var service = CreateService();

        var result = await service.AcceptInvitationAsync(invitation.Token, partner.Id);

        Assert.NotNull(result);
        Assert.Equal(partner.Id, result!.PartnerId);
        var refreshed = await _context.ProjectInvitations.FirstAsync(i => i.Id == invitation.Id);
        Assert.NotNull(refreshed.AcceptedAt);
    }

    // ---------- GetInvitationProjectAsync ----------

    [Fact]
    public async Task GetInvitationProjectAsync_ReturnsNull_WhenExpired()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, "p@example.com", expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var service = CreateService();

        Assert.Null(await service.GetInvitationProjectAsync(invitation.Token));
    }

    [Fact]
    public async Task GetInvitationProjectAsync_ReturnsProject_WhenValid()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var invitation = SeedInvitation(project.Id, "p@example.com");
        var service = CreateService();

        var result = await service.GetInvitationProjectAsync(invitation.Token);

        Assert.NotNull(result);
        Assert.Equal(project.Id, result!.Id);
    }

    // ---------- GetProjectAsync ----------

    [Fact]
    public async Task GetProjectAsync_ReturnsNull_WhenNotFound()
    {
        var service = CreateService();

        Assert.Null(await service.GetProjectAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProjectAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetProjectAsync(project.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProjectAsync_ReturnsProject_ForParticipant()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        Assert.NotNull(await service.GetProjectAsync(project.Id, partner.Id));
    }

    // ---------- GetUserProjectsAsync ----------

    [Fact]
    public async Task GetUserProjectsAsync_ReturnsProjectsWhereUserIsCreatorOrPartner()
    {
        var user = SeedUser("user@example.com");
        var other = SeedUser("other@example.com");
        SeedProject(user.Id);
        SeedProject(other.Id, partnerId: user.Id);
        SeedProject(other.Id);
        var service = CreateService();

        var projects = await service.GetUserProjectsAsync(user.Id);

        Assert.Equal(2, projects.Count);
        Assert.All(projects, p => Assert.True(p.CreatorId == user.Id || p.PartnerId == user.Id));
    }

    // ---------- ApproveProjectAsync ----------

    [Fact]
    public async Task ApproveProjectAsync_Throws_WhenProjectNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ApproveProjectAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task ApproveProjectAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ApproveProjectAsync(project.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task ApproveProjectAsync_Throws_WhenNoPartner()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveProjectAsync(project.Id, creator.Id));
    }

    [Fact]
    public async Task ApproveProjectAsync_SetsStatusActive_OnSuccess()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id);
        var service = CreateService();

        await service.ApproveProjectAsync(project.Id, creator.Id);

        var refreshed = await _context.Projects.FirstAsync(p => p.Id == project.Id);
        Assert.Equal(ProjectStatus.Active, refreshed.Status);
    }

    // ---------- ProposeChangeAsync ----------

    [Fact]
    public async Task ProposeChangeAsync_Throws_WhenProjectNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProposeChangeAsync(
            Guid.NewGuid(), Guid.NewGuid(), ChangeRequestType.EndDate, "{}"));
    }

    [Fact]
    public async Task ProposeChangeAsync_Throws_WhenUserNotParticipant()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id, status: ProjectStatus.Active);
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ProposeChangeAsync(
            project.Id, Guid.NewGuid(), ChangeRequestType.EndDate, "{}"));
    }

    [Theory]
    [InlineData(ProjectStatus.Finished)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task ProposeChangeAsync_Throws_WhenProjectFinishedOrCancelled(ProjectStatus status)
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id, status: status);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProposeChangeAsync(
            project.Id, creator.Id, ChangeRequestType.EndDate, "{}"));
    }

    [Fact]
    public async Task ProposeChangeAsync_Throws_WhenPendingRequestExists()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id, status: ProjectStatus.Active);
        _context.ProjectChangeRequests.Add(new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            RequestedByUserId = creator.Id,
            Type = ChangeRequestType.EndDate,
            PayloadJson = "{}",
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProposeChangeAsync(
            project.Id, creator.Id, ChangeRequestType.Frequency, "{}"));
    }

    [Fact]
    public async Task ProposeChangeAsync_CreatesPendingRequest_OnSuccess()
    {
        var creator = SeedUser("creator@example.com");
        var project = SeedProject(creator.Id, status: ProjectStatus.Active);
        var service = CreateService();

        var cr = await service.ProposeChangeAsync(
            project.Id, creator.Id, ChangeRequestType.Frequency, "{\"Frequency\":1}");

        Assert.Equal(ChangeRequestStatus.Pending, cr.Status);
        Assert.Equal(creator.Id, cr.RequestedByUserId);
        Assert.Single(_context.ProjectChangeRequests);
    }

    // ---------- RespondToChangeRequestAsync ----------

    private ProjectChangeRequest SeedChangeRequest(Guid projectId, Guid requestedBy, ChangeRequestType type, string payload)
    {
        var cr = new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RequestedByUserId = requestedBy,
            Type = type,
            PayloadJson = payload,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectChangeRequests.Add(cr);
        _context.SaveChanges();
        return cr;
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_Throws_WhenProjectNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.RespondToChangeRequestAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true));
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_Throws_WhenChangeRequestNotFound()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.RespondToChangeRequestAsync(
            project.Id, Guid.NewGuid(), partner.Id, true));
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_Throws_WhenRespondingToOwnRequest()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.Frequency, "{}");
        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RespondToChangeRequestAsync(
            project.Id, cr.Id, creator.Id, true));
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_Throws_WhenRequestNoLongerPending()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.Frequency, "{}");
        cr.Status = ChangeRequestStatus.Approved;
        _context.SaveChanges();
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RespondToChangeRequestAsync(
            project.Id, cr.Id, partner.Id, true));
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_Reject_SetsStatusRejected()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var originalFreq = project.Frequency;
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.Frequency,
            JsonSerializer.Serialize(new { Frequency = ProjectFrequency.Monthly }));
        var service = CreateService();

        await service.RespondToChangeRequestAsync(project.Id, cr.Id, partner.Id, approve: false);

        var refreshedCr = await _context.ProjectChangeRequests.FirstAsync(x => x.Id == cr.Id);
        var refreshedProject = await _context.Projects.FirstAsync(p => p.Id == project.Id);
        Assert.Equal(ChangeRequestStatus.Rejected, refreshedCr.Status);
        Assert.Equal(originalFreq, refreshedProject.Frequency);
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_ApproveEndDate_UpdatesEndDate()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var newEnd = DateTime.UtcNow.AddDays(90).Date;
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.EndDate,
            JsonSerializer.Serialize(new { EndDate = newEnd }));
        var service = CreateService();

        await service.RespondToChangeRequestAsync(project.Id, cr.Id, partner.Id, approve: true);

        var refreshedProject = await _context.Projects.FirstAsync(p => p.Id == project.Id);
        var refreshedCr = await _context.ProjectChangeRequests.FirstAsync(x => x.Id == cr.Id);
        Assert.Equal(ChangeRequestStatus.Approved, refreshedCr.Status);
        Assert.Equal(newEnd, refreshedProject.EndDate.Date);
        Assert.Equal(DateTimeKind.Utc, refreshedProject.EndDate.Kind);
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_ApproveFrequency_UpdatesFrequency()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.Frequency,
            JsonSerializer.Serialize(new { Frequency = ProjectFrequency.Monthly }));
        var service = CreateService();

        await service.RespondToChangeRequestAsync(project.Id, cr.Id, partner.Id, approve: true);

        var refreshedProject = await _context.Projects.FirstAsync(p => p.Id == project.Id);
        Assert.Equal(ProjectFrequency.Monthly, refreshedProject.Frequency);
    }

    [Fact]
    public async Task RespondToChangeRequestAsync_ApproveGoals_ReplacesGoals()
    {
        var creator = SeedUser("creator@example.com");
        var partner = SeedUser("partner@example.com");
        var project = SeedProject(creator.Id, partner.Id, ProjectStatus.Active);
        _context.GoalFields.Add(new GoalField
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Label = "Old",
            DataType = GoalDataType.Integer,
            Unit = "count"
        });
        _context.SaveChanges();

        var payload = JsonSerializer.Serialize(new
        {
            Goals = new[]
            {
                new { Label = "New Goal", DataType = GoalDataType.Decimal, Unit = "kg", MinValue = (decimal?)0, MaxValue = (decimal?)100, TargetValue = (decimal?)50 }
            }
        });
        var cr = SeedChangeRequest(project.Id, creator.Id, ChangeRequestType.Goals, payload);
        // Detach seeded entities so the service loads them fresh, as it would in
        // a real request handled by a scoped DbContext.
        _context.ChangeTracker.Clear();
        var service = CreateService();

        await service.RespondToChangeRequestAsync(project.Id, cr.Id, partner.Id, approve: true);

        var goals = await _context.GoalFields.Where(g => g.ProjectId == project.Id).ToListAsync();
        var goal = Assert.Single(goals);
        Assert.Equal("New Goal", goal.Label);
        Assert.Equal("kg", goal.Unit);
    }
}
