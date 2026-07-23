using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Services;

public interface IProjectService
{
    Task<Project> CreateProjectAsync(
        Guid creatorId,
        string name,
        DateTime startDate,
        DateTime endDate,
        ProjectFrequency frequency,
        IEnumerable<GoalFieldInput> goals,
        CancellationToken cancellationToken = default);

    Task<ProjectInvitation> InvitePartnerAsync(
        Guid projectId,
        Guid inviterUserId,
        string partnerEmail,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task<Project?> AcceptInvitationAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Project?> GetInvitationProjectAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<Project?> GetProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<Project>> GetUserProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task ApproveProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ProjectChangeRequest> ProposeChangeAsync(
        Guid projectId,
        Guid userId,
        ChangeRequestType type,
        string payloadJson,
        CancellationToken cancellationToken = default);

    Task RespondToChangeRequestAsync(
        Guid projectId,
        Guid changeRequestId,
        Guid userId,
        bool approve,
        CancellationToken cancellationToken = default);
}

public sealed record GoalFieldInput(
    string Label,
    GoalDataType DataType,
    string Unit,
    decimal? MinValue,
    decimal? MaxValue,
    decimal? TargetValue);
