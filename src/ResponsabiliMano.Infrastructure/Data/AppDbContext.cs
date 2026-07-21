using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Core.Entities;

namespace ResponsabiliMano.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<GoalField> GoalFields => Set<GoalField>();
    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();
    public DbSet<ProjectChangeRequest> ProjectChangeRequests => Set<ProjectChangeRequest>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<CheckInMetric> CheckInMetrics => Set<CheckInMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Frequency).HasColumnName("frequency");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.HasOne(e => e.Creator).WithMany(u => u.OwnedProjects).HasForeignKey(e => e.CreatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Partner).WithMany(u => u.PartnerProjects).HasForeignKey(e => e.PartnerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GoalField>(entity =>
        {
            entity.ToTable("goal_fields");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Label).HasColumnName("label").IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataType).HasColumnName("data_type");
            entity.Property(e => e.Unit).HasColumnName("unit").IsRequired().HasMaxLength(50);
            entity.Property(e => e.MinValue).HasColumnName("min_value");
            entity.Property(e => e.MaxValue).HasColumnName("max_value");
            entity.Property(e => e.TargetValue).HasColumnName("target_value");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.HasOne(e => e.Project).WithMany(p => p.Goals).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectInvitation>(entity =>
        {
            entity.ToTable("project_invitations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
            entity.Property(e => e.Token).HasColumnName("token").IsRequired().HasMaxLength(256);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            entity.HasOne(e => e.Project).WithMany(p => p.Invitations).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Token).IsUnique();
        });

        modelBuilder.Entity<ProjectChangeRequest>(entity =>
        {
            entity.ToTable("project_change_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasOne(e => e.Project).WithMany(p => p.ChangeRequests).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ProjectId, e.Status });
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("check_ins");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Feeling).HasColumnName("feeling");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.PeriodNumber).HasColumnName("period_number");
            entity.HasOne(e => e.Project).WithMany(p => p.CheckIns).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ProjectId, e.UserId, e.PeriodNumber }).IsUnique();
        });

        modelBuilder.Entity<CheckInMetric>(entity =>
        {
            entity.ToTable("check_in_metrics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CheckInId).HasColumnName("check_in_id");
            entity.Property(e => e.GoalFieldId).HasColumnName("goal_field_id");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.HasOne(e => e.CheckIn).WithMany(c => c.Metrics).HasForeignKey(e => e.CheckInId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.GoalField).WithMany(g => g.Metrics).HasForeignKey(e => e.GoalFieldId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.CheckInId, e.GoalFieldId }).IsUnique();
        });
    }
}
