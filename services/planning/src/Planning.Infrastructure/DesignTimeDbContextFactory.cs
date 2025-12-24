using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planning.Infrastructure;

/// <summary>
/// Factory for creating PlanningDbContext at design time (EF migrations).
/// This bypasses the need for Redis and other runtime dependencies.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    public PlanningDbContext CreateDbContext(string[] args)
    {
        // Try to load from environment variable first, fallback to local dev defaults
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=55432;Database=planningdb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PlanningDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("Planning.Infrastructure");
        });

        return new PlanningDbContext(optionsBuilder.Options);
    }
}
