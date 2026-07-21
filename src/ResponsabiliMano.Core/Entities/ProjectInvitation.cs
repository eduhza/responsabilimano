namespace ResponsabiliMano.Core.Entities;

public class ProjectInvitation
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public Project Project { get; set; } = null!;
}
