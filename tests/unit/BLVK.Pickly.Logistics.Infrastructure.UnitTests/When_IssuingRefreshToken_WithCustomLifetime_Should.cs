using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_IssuingRefreshToken_WithCustomLifetime_Should
{
    [Fact]
    public async Task UseRequestedExpiry()
    {
        var now = new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero);
        var lifetime = TimeSpan.FromDays(1);
        var user = AppUser.Create("jane@example.com");
        var repository = Substitute.For<IRepository<AuthenticationRefreshToken>>();
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        identityService.GetSecurityStampAsync(user).Returns("security-stamp");
        var service = new RefreshTokenService(
            repository,
            identityService,
            Options.Create(new RefreshTokenOptions()),
            new FakeTimeProvider(now));

        var sessionId = Guid.CreateVersion7();
        var result = await service.IssueAsync(user, sessionId, lifetime, CancellationToken.None);

        result.ExpiresAtUtc.ShouldBe(now.Add(lifetime));
        await repository.Received(1).AddAsync(Arg.Is<AuthenticationRefreshToken>(token =>
            token.AppUserId == user.Id
            && token.AuthenticationSessionId == sessionId
            && token.ExpiresAtUtc == now.Add(lifetime)));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
