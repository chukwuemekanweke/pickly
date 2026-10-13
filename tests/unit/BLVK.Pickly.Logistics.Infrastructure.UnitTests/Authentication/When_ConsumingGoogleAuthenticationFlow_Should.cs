using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Authentication;

public sealed class When_ConsumingGoogleAuthenticationFlow_Should
{
    [Fact]
    public async Task RejectReplay()
    {
        var cache = new InMemoryJsonCache();
        var clock = new FixedTimeProvider();
        var service = new GoogleAuthenticationFlowService(
            cache,
            Options.Create(new GoogleAuthenticationOptions { Enabled = true, ClientIds = ["client-id"] }),
            clock);
        var flow = await service.StartAsync(Guid.CreateVersion7(), CancellationToken.None);
        var firstTake = await service.TakeAsync(flow.Token, CancellationToken.None);
        await service.ConsumeAsync(firstTake.Flow!, CancellationToken.None);

        var replay = await service.TakeAsync(flow.Token, CancellationToken.None);

        replay.Status.ShouldBe(GoogleAuthenticationFlowStatus.Consumed);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class InMemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _values = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = _values.Remove(key, out var removed) ? (T?)removed : default;
            return Task.FromResult(value);
        }
    }
}
