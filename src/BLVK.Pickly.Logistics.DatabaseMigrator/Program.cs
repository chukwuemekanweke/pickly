using BLVK.Pickly.Logistics.DatabaseMigrator.Healthcheck;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddSingleton<DatabaseMigrationState>();
builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseMigratorReadinessHealthCheck>(
        "database-migrator-readiness",
        tags: ["readiness"])
    .AddCheck<DatabaseMigratorLivenessHealthCheck>(
        "database-migrator-liveness",
        tags: ["liveness"]);

var app = builder.Build();

app.MapHealthChecks("/health/readiness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("readiness")
});

app.MapHealthChecks("/health/liveness", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("liveness")
});

app.MapGet("/", () => Results.Ok(new
{
    Service = "BLVK.Pickly.Logistics.DatabaseMigrator",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();
