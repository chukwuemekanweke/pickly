using BLVK.Pickly.Logistics.Domain.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Infrastructure.Caching;

public sealed class DistributedJsonCache(IDistributedCache distributedCache) : IJsonCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var payload = await distributedCache.GetStringAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        distributedCache.RemoveAsync(key, cancellationToken);

    public async Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await GetAsync<T>(key, cancellationToken);
        if (value is not null)
        {
            await RemoveAsync(key, cancellationToken);
        }

        return value;
    }
}
