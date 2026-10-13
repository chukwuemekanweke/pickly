namespace BLVK.Pickly.Logistics.Domain.Common.Caching;

public interface IJsonCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
    Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken);
}
