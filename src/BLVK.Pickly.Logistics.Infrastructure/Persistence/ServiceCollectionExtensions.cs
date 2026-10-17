using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresWritePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentActorAccessor, CurrentActorAccessor>();
        services.AddScoped<ICurrentActor>(serviceProvider => serviceProvider.GetRequiredService<ICurrentActorAccessor>());
        services.AddScoped<AuditAndSoftDeleteInterceptor>();
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                    GetRequiredConnectionString(configuration, "PostgresWrite"),
                    npgsql => npgsql.UseNetTopologySuite())
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditAndSoftDeleteInterceptor>(),
                    serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IPickupLocationConsistencyLock, PickupLocationConsistencyLock>();
        services.AddScoped<IDeliveryBookingPersistence, DeliveryBookingPersistence>();
        services.AddScoped<ILocationRegistrationStore, LocationRegistrationStore>();
        services.AddScoped<ILocationResolutionLease, LocationResolutionLease>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        return services;
    }

    public static IServiceCollection AddPostgresReadPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ObservabilityCommandInterceptor>();

        services.AddDbContext<AppReadDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                    GetReadConnectionString(configuration),
                    npgsql => npgsql.UseNetTopologySuite())
                .AddInterceptors(serviceProvider.GetRequiredService<ObservabilityCommandInterceptor>()));

        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        services.AddScoped<IStakeholderReadModelRepository, StakeholderReadModelRepository>();
        services.AddScoped<IWalletTransactionReadModelRepository, WalletTransactionReadModelRepository>();
        services.AddScoped<ILoginActivityReadModelRepository, LoginActivityReadModelRepository>();
        services.AddScoped<IActiveSessionReadModelRepository, ActiveSessionReadModelRepository>();
        services.AddScoped<ISavedPickupLocationReadModelRepository, SavedPickupLocationReadModelRepository>();
        services.AddScoped<ILocationReadModelRepository, LocationReadModelRepository>();
        services.AddScoped<IDeliveryZoneReadModelRepository, DeliveryZoneReadModelRepository>();

        return services;
    }

    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddPostgresWritePersistence(configuration)
            .AddPostgresReadPersistence(configuration);

    private static string GetReadConnectionString(IConfiguration configuration) =>
        GetRequiredConnectionString(configuration, "PostgresRead");

    internal static string GetRequiredConnectionString(IConfiguration configuration, string name) =>
        PostgresConnectionString.Normalize(
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required."));
}
