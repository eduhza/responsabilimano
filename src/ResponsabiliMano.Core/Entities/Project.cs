using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CreatorId { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ProjectFrequency Frequency { get; set; }
    public ProjectStatus Status { get; set; }

    public User Creator { get; set; } = null!;
    public User? Partner { get; set; }
    public ICollection<GoalField> Goals { get; set; } = new List<GoalField>();
    public ICollection<ProjectInvitation> Invitations { get; set; } = new List<ProjectInvitation>();
    public ICollection<ProjectChangeRequest> ChangeRequests { get; set; } = new List<ProjectChangeRequest>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
