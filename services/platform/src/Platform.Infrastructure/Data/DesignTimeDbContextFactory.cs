using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Platform.Infrastructure.Data;

/// <summary>
/// Factory for creating PlatformDbContext at design time (EF migrations).
/// This bypasses the need for Redis and other runtime dependencies.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        // Try to load from environment variable first, fallback to local dev defaults
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=calendarun_platform;Username=calendarun;Password=calendarun";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("Platform.Infrastructure");
        });

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
