using ResponsabiliMano.Core.Entities;

namespace ResponsabiliMano.Core.Services;

public interface IProjectService
{
    Task<Project> CreateProjectAsync(
        Guid creatorId,
        string name,
        DateTime startDate,
        DateTime endDate,
        Core.Enums.ProjectFrequency frequency,
        IEnumerable<GoalFieldInput> goals,
        CancellationToken cancellationToken = default);
}

public sealed record GoalFieldInput(
    string Label,
    Core.Enums.GoalDataType DataType,
    string Unit,
    decimal? MinValue,
    decimal? MaxValue,
    decimal? TargetValue);
