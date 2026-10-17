using Asp.Versioning;
using BLVK.Pickly.Logistics.Application;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using BLVK.Pickly.Logistics.Infrastructure.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using BLVK.Pickly.Logistics.Infrastructure.Locations;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using BLVK.Pickly.Logistics.Infrastructure.Observability;
using BLVK.Pickly.Logistics.Infrastructure.Payments;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Storage;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.ApiDocumentation;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsProduction())
        {
            policy
                .WithOrigins("http://localhost:9000", "https://localhost:9000")
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddValidatorsFromAssemblyContaining<SignUpValidator>();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddApiDocumentation();
builder.Services.AddApplication();
var quoteOptions = builder.Configuration.GetSection(DeliveryQuoteOptions.SectionName).Get<DeliveryQuoteOptions>() ?? new DeliveryQuoteOptions();
quoteOptions.Validate();
builder.Services.Configure<DeliveryQuoteOptions>(builder.Configuration.GetSection(DeliveryQuoteOptions.SectionName));
var dispatchOptions = builder.Configuration.GetSection(DeliveryDispatchOptions.SectionName).Get<DeliveryDispatchOptions>() ?? new DeliveryDispatchOptions();
dispatchOptions.Validate();
builder.Services.Configure<DeliveryDispatchOptions>(builder.Configuration.GetSection(DeliveryDispatchOptions.SectionName));
builder.Services.AddPostgresWritePersistence(builder.Configuration);
builder.Services.AddPostgresReadPersistence(builder.Configuration);
builder.Services.AddIdentityUserManagement(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddIpGeolocationServices(builder.Configuration);
builder.Services.AddLocationServices(builder.Configuration);
builder.Services.AddDeliveryServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRequestRateLimiting(builder.Configuration);
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services.AddObjectStorage(builder.Configuration);
builder.Services.AddPaymentServices(builder.Configuration);
builder.Services.AddNotificationWebhookServices(builder.Configuration);
builder.Services.AddTransactionalOutbox();
builder.Services.AddCustomTelemetryContext();
builder.Services.AddBackendTelemetry(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<CurrentActorMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.UseApiDocumentation();
app.MapPrometheusScrapingEndpoint("/metrics").DisableRateLimiting();
app.MapHealthChecks("/health").DisableRateLimiting();
app.MapControllers();
app.MapGet("/", () => TypedResults.Ok(new
{
    Service = "BLVK.Pickly.Logistics.WebAPI",
    Status = "Healthy"
}))
.ExcludeFromDescription();

if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    await app.InitializeDatabaseAsync();
}

app.Run();

public partial class Program;
