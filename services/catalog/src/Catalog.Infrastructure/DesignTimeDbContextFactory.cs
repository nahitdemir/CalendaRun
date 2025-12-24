using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

/// <summary>
/// Factory for creating CatalogDbContext at design time (EF migrations).
/// This bypasses the need for Redis and other runtime dependencies.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        // Try to load from environment variable first, fallback to local dev defaults
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=calendarun_catalog;Username=calendarun;Password=calendarun";

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("Catalog.Infrastructure");
        });

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
