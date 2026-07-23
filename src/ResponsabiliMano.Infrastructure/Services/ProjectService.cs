using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class ProjectService : IProjectService
{
    private readonly AppDbContext _context;

    public ProjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project> CreateProjectAsync(
        Guid creatorId,
        string name,
        DateTime startDate,
        DateTime endDate,
        ProjectFrequency frequency,
        IEnumerable<GoalFieldInput> goals,
        CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatorId = creatorId,
            StartDate = startDate,
            EndDate = endDate,
            Frequency = frequency,
            Status = ProjectStatus.Pending
        };

        foreach (var goal in goals)
        {
            if (string.IsNullOrWhiteSpace(goal.Label))
                throw new ArgumentException("Goal label is required.");

            if (string.IsNullOrWhiteSpace(goal.Unit))
                throw new ArgumentException("Goal unit is required.");

            project.Goals.Add(new GoalField
            {
                Id = Guid.NewGuid(),
                Label = goal.Label.Trim(),
                DataType = goal.DataType,
                Unit = goal.Unit.Trim(),
                MinValue = goal.MinValue,
                MaxValue = goal.MaxValue,
                TargetValue = goal.TargetValue
            });
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }
}
