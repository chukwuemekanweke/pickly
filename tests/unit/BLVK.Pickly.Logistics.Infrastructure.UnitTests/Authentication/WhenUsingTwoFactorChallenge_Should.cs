using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Collections.Concurrent;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

public sealed class WhenUsingTwoFactorChallenge_Should
{
    [Fact]
    public async Task ConsumeOpaqueChallengeOnlyOnce()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero));
        var service = new TwoFactorChallengeService(
            new InMemoryJsonCache(clock),
            Options.Create(new TwoFactorAuthenticationOptions()),
            clock);

        var challenge = await service.StartAsync(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            AuthenticationMethod.Password, new ActorContext(null, null, "correlation", "flow"),
            "127.0.0.1", "agent", CancellationToken.None);
        var first = await service.TakeAsync(challenge.Token, CancellationToken.None);
        var replay = await service.TakeAsync(challenge.Token, CancellationToken.None);

        challenge.Token.Length.ShouldBeGreaterThan(32);
        first.Status.ShouldBe(TwoFactorChallengeStatus.Success);
        replay.Status.ShouldBe(TwoFactorChallengeStatus.Consumed);
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryJsonCache(TimeProvider timeProvider) : IJsonCache
    {
        private readonly ConcurrentDictionary<string, CacheItem> _items = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            if (!_items.TryGetValue(key, out var item) || item.ExpiresAtUtc <= timeProvider.GetUtcNow())
                return Task.FromResult<T?>(default);
            return Task.FromResult((T?)item.Value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _items[key] = new CacheItem(value!, timeProvider.GetUtcNow().Add(ttl));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _items.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
        {
            if (!_items.TryRemove(key, out var item) || item.ExpiresAtUtc <= timeProvider.GetUtcNow())
                return Task.FromResult<T?>(default);
            return Task.FromResult((T?)item.Value);
        }

        private sealed record CacheItem(object Value, DateTimeOffset ExpiresAtUtc);
    }
}
