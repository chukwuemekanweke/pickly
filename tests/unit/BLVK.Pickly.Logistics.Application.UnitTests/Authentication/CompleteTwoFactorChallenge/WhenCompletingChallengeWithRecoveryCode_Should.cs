using BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenCompletingChallengeWithRecoveryCode_Should
{
    [Fact]
    public async Task RedeemIdentityRecoveryCode()
    {
        var context = new AuthenticationFlowTestContext();
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var challenge = new TwoFactorChallenge("opaque", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Password, new ActorContext(null, stakeholder.TenantId, "c", "f"),
            "127.0.0.1", "agent", context.Clock.GetUtcNow().AddMinutes(5), 5);
        context.TwoFactorChallengeService.TakeAsync(challenge.Token, Arg.Any<CancellationToken>())
            .Returns(new TwoFactorChallengeResult(TwoFactorChallengeStatus.Success, challenge));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.RedeemTwoFactorRecoveryCodeAsync(user, "recovery-code")
            .Returns(IdentityResult.Success);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, Arg.Any<Guid>())
            .Returns(new AccessToken("access", context.Clock.GetUtcNow().AddMinutes(5)));
        context.RefreshTokenService.IssueAsync(user, Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new RefreshToken("refresh", context.Clock.GetUtcNow().AddDays(30)));

        var result = await context.CreateCompleteTwoFactorChallengeHandler().HandleAsync(
            new CompleteTwoFactorChallengeCommand(challenge.Token,
                TwoFactorVerificationMethod.RecoveryCode, "recovery-code"), CancellationToken.None);

        result.Status.ShouldBe(CompleteTwoFactorChallengeStatus.Success);
        await context.IdentityService.Received(1)
            .RedeemTwoFactorRecoveryCodeAsync(user, "recovery-code");
        await context.IdentityService.DidNotReceiveWithAnyArgs()
            .VerifyAuthenticatorTokenAsync(default!, default!);
    }
}
