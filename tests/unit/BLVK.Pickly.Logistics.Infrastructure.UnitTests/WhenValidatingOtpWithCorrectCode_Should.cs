using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenValidatingOtpWithCorrectCode_Should
{
    [Fact]
    public async Task SucceedAndClearOtp()
    {
        var cache = new InMemoryJsonCache();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 4, 17, 0, 0, 0, TimeSpan.Zero));
        var service = new TwoFactorOtpService(cache, clock);
        var userId = Guid.CreateVersion7();
        var generatedOtp = await service.GenerateOtpAsync(
            userId,
            OtpIntent.PasswordReset,
            CancellationToken.None,
            6,
            false);

        var result = await service.ValidateOtpAsync(
            userId,
            generatedOtp.Code.ToLowerInvariant(),
            OtpIntent.PasswordReset,
            CancellationToken.None);

        result.ShouldBeTrue();
        (await service.OtpExistsAsync(userId, OtpIntent.PasswordReset, CancellationToken.None)).ShouldBeFalse();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _store = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _store[key] = value!;
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

