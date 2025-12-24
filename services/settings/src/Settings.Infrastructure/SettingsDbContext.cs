using Microsoft.EntityFrameworkCore;
using Settings.Domain;

namespace Settings.Infrastructure;

public class SettingsDbContext : DbContext
{
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options) { }

    public DbSet<SettingDefinition> SettingDefinitions => Set<SettingDefinition>();
    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
    public DbSet<SettingAudit> SettingAudits => Set<SettingAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SettingDefinition>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ValueType).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<SettingValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TenantId).HasMaxLength(100);
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ValueJson).IsRequired();
            
            entity.HasOne(e => e.Definition)
                .WithMany()
                .HasForeignKey(e => e.Key)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => new { e.Key, e.TenantId, e.Environment }).IsUnique();
        });

        modelBuilder.Entity<SettingAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TenantId).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.HasIndex(e => e.UpdatedAt);
        });

        // Seed default settings definitions
        var now = DateTimeOffset.UtcNow;
        modelBuilder.Entity<SettingDefinition>().HasData(
            new SettingDefinition { Key = "notifications.smtp.host", Description = "SMTP Host", ValueType = SettingValueType.String, IsRequired = true, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.smtp.port", Description = "SMTP Port", ValueType = SettingValueType.Integer, IsRequired = true, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.smtp.from", Description = "Default From Email", ValueType = SettingValueType.String, IsRequired = true, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.email.subject_template", Description = "Email Subject Template", ValueType = SettingValueType.String, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.email.body_template", Description = "Email Body Template", ValueType = SettingValueType.String, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.reminder.offsets_minutes", Description = "Reminder offsets in minutes", ValueType = SettingValueType.StringArray, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.dispatcher.max_attempts", Description = "Max retry attempts for notifications", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.dispatcher.batch_size", Description = "Batch size for notification dispatcher", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "notifications.dispatcher.retry_backoff_base_seconds", Description = "Base seconds for exponential retry backoff", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "planning.default_timezone", Description = "Default Timezone", ValueType = SettingValueType.String, IsRequired = true, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "planning.max_plans_per_user", Description = "Max plans per user", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "platform.invite.expiration_days", Description = "Invite expiration in days", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "platform.membership.cache_ttl_minutes", Description = "Membership validation cache TTL in minutes", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "gateway.http_client.timeout_seconds", Description = "Gateway HTTP client timeout in seconds", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now },
            new SettingDefinition { Key = "audit.default_page_size", Description = "Default page size for audit log queries", ValueType = SettingValueType.Integer, IsRequired = false, CreatedAt = now, UpdatedAt = now }
        );

        // Seed default values for dev environment
        modelBuilder.Entity<SettingValue>().HasData(
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000001"), Key = "notifications.smtp.host", Environment = "dev", ValueJson = "\"localhost\"", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000002"), Key = "notifications.smtp.port", Environment = "dev", ValueJson = "1025", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000003"), Key = "notifications.smtp.from", Environment = "dev", ValueJson = "\"noreply@calendarun.local\"", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000004"), Key = "notifications.email.subject_template", Environment = "dev", ValueJson = "\"You planned event: {EventId}\"", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000005"), Key = "notifications.email.body_template", Environment = "dev", ValueJson = "\"Hello!\\n\\nYou have planned event {EventId}.\\nPlan ID: {PlanItemId}\\n\\nBest regards,\\nCalendaRun Team\"", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000006"), Key = "notifications.reminder.offsets_minutes", Environment = "dev", ValueJson = "[1440, 60, 15]", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000009"), Key = "notifications.dispatcher.max_attempts", Environment = "dev", ValueJson = "3", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000010"), Key = "notifications.dispatcher.batch_size", Environment = "dev", ValueJson = "50", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000011"), Key = "notifications.dispatcher.retry_backoff_base_seconds", Environment = "dev", ValueJson = "10", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000007"), Key = "planning.default_timezone", Environment = "dev", ValueJson = "\"Europe/Istanbul\"", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000008"), Key = "planning.max_plans_per_user", Environment = "dev", ValueJson = "100", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000012"), Key = "platform.invite.expiration_days", Environment = "dev", ValueJson = "7", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000013"), Key = "platform.membership.cache_ttl_minutes", Environment = "dev", ValueJson = "5", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000014"), Key = "gateway.http_client.timeout_seconds", Environment = "dev", ValueJson = "5", Version = 1, UpdatedAt = now },
            new SettingValue { Id = Guid.Parse("11111111-0001-0001-0001-000000000015"), Key = "audit.default_page_size", Environment = "dev", ValueJson = "50", Version = 1, UpdatedAt = now }
        );
    }
}

