using BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenCompletingAuthenticatorChallenge_Should
{
    [Fact]
    public async Task CreateSessionAndPublishSuccessfulSignIn()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var actorContext = new ActorContext(null, stakeholder.TenantId, "correlation", "flow");
        var challenge = new TwoFactorChallenge("opaque", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Password, actorContext, "127.0.0.1", "agent",
            context.Clock.GetUtcNow().AddMinutes(5), 5);
        var accessToken = new AccessToken("access", context.Clock.GetUtcNow().AddMinutes(5));
        var refreshToken = new RefreshToken("refresh", context.Clock.GetUtcNow().AddDays(30));
        context.TwoFactorChallengeService.TakeAsync(challenge.Token, Arg.Any<CancellationToken>())
            .Returns(new TwoFactorChallengeResult(TwoFactorChallengeStatus.Success, challenge));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, Arg.Any<Guid>()).Returns(accessToken);
        context.RefreshTokenService.IssueAsync(user, Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(refreshToken);

        var result = await context.CreateCompleteTwoFactorChallengeHandler().HandleAsync(
            new CompleteTwoFactorChallengeCommand(challenge.Token,
                TwoFactorVerificationMethod.Authenticator, "123456"),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteTwoFactorChallengeStatus.Success);
        result.Tokens.ShouldNotBeNull();
        await context.SessionService.Received(1).CreateAsync(stakeholder, challenge.IpAddress,
            challenge.UserAgent, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message => message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
    }
}
