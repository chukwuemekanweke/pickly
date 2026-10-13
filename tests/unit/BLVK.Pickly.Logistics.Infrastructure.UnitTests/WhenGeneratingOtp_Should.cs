using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenGeneratingOtp_Should
{
    [Fact]
    public async Task StoreAnOtpThatExists()
    {
        var cache = new InMemoryJsonCache();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 17, 0, 0, 0, TimeSpan.Zero));
        var service = new TwoFactorOtpService(cache, clock);
        var userId = Guid.CreateVersion7();
        var expiresAtUtc = clock.GetUtcNow().AddSeconds(90);

        var otp = await service.GenerateOtpAsync(
            userId,
            OtpIntent.PasswordReset,
            CancellationToken.None,
            6,
            false,
            expiresAtUtc);

        otp.Code.Length.ShouldBe(6);
        otp.Code.All(char.IsDigit).ShouldBeTrue();
        otp.ExpiresAtUtc.ShouldBe(expiresAtUtc);
        cache.LastTtl.ShouldBe(TimeSpan.FromSeconds(90));
        (await service.OtpExistsAsync(userId, OtpIntent.PasswordReset, CancellationToken.None)).ShouldBeTrue();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _store = [];

        public TimeSpan? LastTtl { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _store[key] = value!;
            LastTtl = ttl;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = _store.Remove(key, out var removed) ? (T?)removed : default;
            return Task.FromResult(value);
        }
    }
}

