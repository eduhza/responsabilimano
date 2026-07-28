using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;

namespace ResponsabiliMano.Web.IntegrationTests;

public static class SeedHelper
{
    public static async Task<(User creator, User partner, Project project)> SeedActiveProjectAsync(
        IntegrationFixture fixture,
        string creatorEmail = "creator@example.com",
        string partnerEmail = "partner@example.com")
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var creator = new User
        {
            Id = Guid.NewGuid(),
            Name = "Creator",
            Email = creatorEmail,
            PasswordHash = PasswordHasher.Hash("Password123!"),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        var partner = new User
        {
            Id = Guid.NewGuid(),
            Name = "Partner",
            Email = partnerEmail,
            PasswordHash = PasswordHasher.Hash("Password123!"),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(creator, partner);

        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            Label = "Steps",
            DataType = GoalDataType.Integer,
            Unit = "count",
            TargetValue = 10000
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            CreatorId = creator.Id,
            PartnerId = partner.Id,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(80),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active
        };
        goal.ProjectId = project.Id;
        project.Goals.Add(goal);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
        return (creator, partner, project);
    }

    public static async Task<(User creator, Project project)> SeedPendingProjectAsync(
        IntegrationFixture fixture,
        string creatorEmail = "creator@example.com")
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var creator = new User
        {
            Id = Guid.NewGuid(),
            Name = "Creator",
            Email = creatorEmail,
            PasswordHash = PasswordHasher.Hash("Password123!"),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(creator);

        var goal = new GoalField
        {
            Id = Guid.NewGuid(),
            Label = "Steps",
            DataType = GoalDataType.Integer,
            Unit = "count",
            TargetValue = 10000
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Pending Project",
            CreatorId = creator.Id,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(29),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Pending
        };
        goal.ProjectId = project.Id;
        project.Goals.Add(goal);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
        return (creator, project);
    }

    public static async Task<(ProjectChangeRequest cr, Project project, User creator, User partner)> SeedChangeRequestAsync(
        IntegrationFixture fixture,
        ChangeRequestType type = ChangeRequestType.Frequency,
        string payloadJson = "{\"Frequency\":2}")
    {
        var (creator, partner, project) = await SeedActiveProjectAsync(fixture);

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cr = new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            RequestedByUserId = creator.Id,
            Type = type,
            PayloadJson = payloadJson,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.ProjectChangeRequests.Add(cr);
        await db.SaveChangesAsync();

        return (cr, project, creator, partner);
    }
}
