using BLVK.Pickly.Logistics.Domain.Common.Caching;
using StackExchange.Redis;

namespace BLVK.Pickly.Logistics.Infrastructure.Caching;

internal sealed class RedisDistributedLease(IConnectionMultiplexer connection) : IDistributedLease
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    public async Task<string?> TryAcquireAsync(string key, TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = Guid.CreateVersion7().ToString("N");
        var acquired = await connection.GetDatabase().StringSetAsync(key, owner, duration, When.NotExists);
        return acquired ? owner : null;
    }

    public async Task ReleaseAsync(string key, string ownerToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await connection.GetDatabase().ScriptEvaluateAsync(ReleaseScript, [key], [ownerToken]);
    }
}
