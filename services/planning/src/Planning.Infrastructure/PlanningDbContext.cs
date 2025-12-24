using Microsoft.EntityFrameworkCore;
using Planning.Domain;

namespace Planning.Infrastructure;

public class PlanningDbContext : DbContext
{
    public PlanningDbContext(DbContextOptions<PlanningDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPlanItem> UserPlanItems => Set<UserPlanItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            
            // Tenant isolation: unique email per tenant
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<UserPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.State)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<PlanState>(v)); // Store enum as string
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Tenant isolation: unique plan per tenant/user/event
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.EventId }).IsUnique();
            entity.HasIndex(e => e.TenantId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ProcessedAt);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActorEmail).HasMaxLength(256);
            entity.Property(e => e.BeforeJson).HasColumnType("jsonb");
            entity.Property(e => e.AfterJson).HasColumnType("jsonb");
            entity.Property(e => e.TraceId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            
            entity.HasIndex(e => new { e.TenantId, e.Timestamp });
            entity.HasIndex(e => new { e.ActorUserId, e.Timestamp });
        });
    }
}
