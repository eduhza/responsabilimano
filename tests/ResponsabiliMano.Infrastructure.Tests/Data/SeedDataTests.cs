using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Data;

/// <summary>
/// Verifies the demo seed fixture (spec S7.5 AC9): a third, non-physical project
/// exists alongside the two fitness projects so the app shows its breadth on first login.
/// </summary>
public class SeedDataTests
{
    [Fact]
    public async Task Seed_creates_three_projects_including_a_non_physical_one()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedData.SeedAsync(context);

        var projects = await context.Projects
            .Include(p => p.Goals)
            .OrderBy(p => p.Name)
            .ToListAsync();

        Assert.Equal(3, projects.Count);

        var coding = projects.Single(p => p.Name == "Code & Carreira");
        Assert.Equal(ProjectStatus.Active, coding.Status);
        Assert.Equal("💻", coding.Icon);
        Assert.Equal(4, coding.Goals.Count);

        // The four goals mirror the "Programação & Carreira" template and exercise
        // the Boolean and Scale types in the seed dashboard.
        Assert.Contains(coding.Goals, g => g.Label == "Horas de código" && g.DataType == GoalDataType.Decimal);
        Assert.Contains(coding.Goals, g => g.Label == "Commits/exercícios" && g.DataType == GoalDataType.Integer);
        Assert.Contains(coding.Goals, g => g.Label == "Estudei algo novo?" && g.DataType == GoalDataType.Boolean);
        Assert.Contains(coding.Goals, g => g.Label == "Confiança técnica" && g.DataType == GoalDataType.Scale);
    }

    [Fact]
    public async Task Seed_is_idempotent_when_users_already_exist()
    {
        await using var context = TestDbContextFactory.Create();

        await SeedData.SeedAsync(context);
        var firstCount = await context.Projects.CountAsync();

        // Second call must be a no-op (SeedAsync returns early when users exist).
        await SeedData.SeedAsync(context);
        var secondCount = await context.Projects.CountAsync();

        Assert.Equal(firstCount, secondCount);
    }
}
