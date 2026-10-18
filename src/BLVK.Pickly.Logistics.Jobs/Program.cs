using BLVK.Pickly.Logistics.Application;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Observability;
using BLVK.Pickly.Logistics.Infrastructure.Payments;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Jobs.Authentication;
using BLVK.Pickly.Logistics.Jobs.HealthChecks;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using BLVK.Pickly.Logistics.Jobs.Observability;
using BLVK.Pickly.Logistics.Jobs.OutboxProcessing;
using BLVK.Pickly.Logistics.Jobs.Payments;
using BLVK.Pickly.Logistics.Jobs.Riders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddBackgroundServiceReadinessTracking();
builder.Services.AddApplication();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddRabbitMqOutboxDispatching(builder.Configuration);
builder.Services.AddOutboxMessageProcessing(builder.Configuration);
builder.Services.AddIpAddressLocationEnrichment(builder.Configuration);
builder.Services.AddPaymentReconciliation(builder.Configuration);
builder.Services.AddRiderMatching(builder.Configuration);
builder.Services.AddJobsHealthChecks();
builder.Services.AddJobsOpenTelemetry(builder.Configuration);

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
    Service = "BLVK.Pickly.Logistics.Jobs",
    Status = "Running"
}))
.ExcludeFromDescription();

await app.RunAsync();

public partial class Program;
