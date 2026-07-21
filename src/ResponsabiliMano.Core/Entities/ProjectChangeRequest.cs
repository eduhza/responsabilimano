using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class ProjectChangeRequest
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ChangeRequestType Type { get; set; }
    public string PayloadJson { get; set; } = null!;
    public ChangeRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
