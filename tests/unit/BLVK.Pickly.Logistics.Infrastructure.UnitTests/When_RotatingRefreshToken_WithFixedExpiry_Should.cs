using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_RotatingRefreshToken_WithFixedExpiry_Should
{
    [Fact]
    public async Task PreserveOriginalExpiry()
    {
        var now = new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero);
        var originalExpiry = now.AddDays(1);
        var user = AppUser.Create("jane@example.com");
        var sessionId = Guid.CreateVersion7();
        var currentToken = AuthenticationRefreshToken.Create(
            user.Id,
            sessionId,
            "current-token-hash",
            "security-stamp",
            originalExpiry);
        var repository = Substitute.For<IRepository<AuthenticationRefreshToken>>();
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        identityService.GetSecurityStampAsync(user).Returns("security-stamp");
        var service = new RefreshTokenService(
            repository,
            identityService,
            Options.Create(new RefreshTokenOptions()),
            new FakeTimeProvider(now));

        var result = await service.RotateAsync(currentToken, user, CancellationToken.None);

        result.ExpiresAtUtc.ShouldBe(originalExpiry);
        currentToken.RevokedAtUtc.ShouldBe(now);
        await repository.Received(1).AddAsync(Arg.Is<AuthenticationRefreshToken>(token =>
            token.AuthenticationSessionId == sessionId));
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
