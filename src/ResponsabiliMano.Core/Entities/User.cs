namespace ResponsabiliMano.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PreferredLanguage { get; set; } = "pt-BR";
    public DateTime CreatedAt { get; set; }

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<Project> PartnerProjects { get; set; } = new List<Project>();
}
