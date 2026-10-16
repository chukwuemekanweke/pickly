using BLVK.Pickly.Logistics.Domain.Common.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BLVK.Pickly.Logistics.Infrastructure.Caching;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJsonCache, DistributedJsonCache>();
        services.AddSingleton<IDistributedLease, RedisDistributedLease>();

        var redisConnectionString = configuration.GetConnectionString("Redis");
        var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString!);
        redisConfiguration.AbortOnConnectFail = false;

        var lazyMultiplexer = new Lazy<IConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(redisConfiguration));

        services.AddSingleton<IConnectionMultiplexer>(_ => lazyMultiplexer.Value);
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(lazyMultiplexer.Value);
        });

        return services;
    }
}
