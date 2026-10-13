using BLVK.Pickly.Logistics.Application.Authentication;
using BLVK.Pickly.Logistics.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenRegeneratingRecoveryCodes_Should
{
    [Fact]
    public async Task ReturnNewCodesAndRevokeOtherSessions()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.IdentityService.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(["new-code-one", "new-code-two"]);
        var handler = new RegenerateTwoFactorRecoveryCodesHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            new TwoFactorProofVerifier(context.IdentityService),
            context.IdentityService,
            context.SessionService,
            Options.Create(new TwoFactorAuthenticationOptions()),
            context.CustomTelemetryContext,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new RegenerateTwoFactorRecoveryCodesCommand(
                TwoFactorVerificationMethod.Authenticator, "123456", sessionId, actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(RegenerateTwoFactorRecoveryCodesStatus.Success);
        result.RecoveryCodes.ShouldBe(["new-code-one", "new-code-two"]);
        await context.IdentityService.DidNotReceiveWithAnyArgs().UpdateSecurityStampAsync(default!);
        await context.SessionService.Received(1)
            .RevokeOthersAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcceptARecoveryCodeAsProof()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var actorContext = new ActorContext(stakeholderId, tenantId, "correlation", "flow");
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(stakeholderId, user.Id, user.Email!, tenantId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe", null, true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.IdentityService.RedeemTwoFactorRecoveryCodeAsync(user, "ABCDE-FGHIJ")
            .Returns(IdentityResult.Success);
        context.IdentityService.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(["new-code-one", "new-code-two"]);
        var handler = new RegenerateTwoFactorRecoveryCodesHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            new TwoFactorProofVerifier(context.IdentityService),
            context.IdentityService,
            context.SessionService,
            Options.Create(new TwoFactorAuthenticationOptions()),
            context.CustomTelemetryContext,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new RegenerateTwoFactorRecoveryCodesCommand(
                TwoFactorVerificationMethod.RecoveryCode, "ABCDE-FGHIJ", sessionId, actorContext),
            CancellationToken.None);

        result.Status.ShouldBe(RegenerateTwoFactorRecoveryCodesStatus.Success);
        await context.IdentityService.Received(1)
            .RedeemTwoFactorRecoveryCodeAsync(user, "ABCDE-FGHIJ");
        await context.IdentityService.DidNotReceiveWithAnyArgs()
            .VerifyAuthenticatorTokenAsync(default!, default!);
    }
}
