using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
        {
            return;
        }

        var userA = new User
        {
            Id = Guid.NewGuid(),
            Name = "Usuário A",
            Email = "a@example.com",
            PasswordHash = "TODO-HASH",
            CreatedAt = DateTime.UtcNow
        };

        var userB = new User
        {
            Id = Guid.NewGuid(),
            Name = "Usuário B",
            Email = "b@example.com",
            PasswordHash = "TODO-HASH",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(userA, userB);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Projeto Demo",
            CreatorId = userA.Id,
            PartnerId = userB.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            Frequency = ProjectFrequency.Weekly,
            Status = ProjectStatus.Active,
            Goals =
            [
                new GoalField
                {
                    Id = Guid.NewGuid(),
                    Label = "Peso (kg)",
                    DataType = GoalDataType.Decimal,
                    Unit = "kg",
                    MinValue = 0,
                    MaxValue = 300,
                    TargetValue = 75
                },
                new GoalField
                {
                    Id = Guid.NewGuid(),
                    Label = "Adesão aos treinos (%)",
                    DataType = GoalDataType.Percent,
                    Unit = "%",
                    MinValue = 0,
                    MaxValue = 100,
                    TargetValue = 80
                }
            ]
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();
    }
}
