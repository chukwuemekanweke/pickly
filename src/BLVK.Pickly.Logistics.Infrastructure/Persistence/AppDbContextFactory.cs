using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgresWrite")
            ?? throw new InvalidOperationException(
                "Environment variable 'ConnectionStrings__PostgresWrite' is required for design-time AppDbContext creation.");

        optionsBuilder.UseNpgsql(PostgresConnectionString.Normalize(connectionString));

        return new AppDbContext(optionsBuilder.Options);
    }
}
