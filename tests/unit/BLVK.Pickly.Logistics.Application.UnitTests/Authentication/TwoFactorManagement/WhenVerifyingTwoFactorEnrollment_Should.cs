using BLVK.Pickly.Logistics.Application.Authentication;
using BLVK.Pickly.Logistics.Application.Authentication.Features.VerifyTwoFactorEnrollment;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenVerifyingTwoFactorEnrollment_Should
{
    [Fact]
    public async Task EnableMfaAndReturnRecoveryCodes()
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
        context.IdentityService.VerifyAuthenticatorTokenAsync(user, "123456").Returns(true);
        context.IdentityService.SetTwoFactorEnabledAsync(user, true).Returns(IdentityResult.Success);
        context.IdentityService.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(["recovery-one", "recovery-two"]);
        var handler = new VerifyTwoFactorEnrollmentHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            context.IdentityService,
            Options.Create(new TwoFactorAuthenticationOptions()),
            context.CustomTelemetryContext,
            context.SessionService,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new VerifyTwoFactorEnrollmentCommand("123456", sessionId, actorContext), CancellationToken.None);

        result.Status.ShouldBe(VerifyTwoFactorEnrollmentStatus.Success);
        result.RecoveryCodes.ShouldBe(["recovery-one", "recovery-two"]);
        await context.SessionService.Received(1)
            .RevokeOthersAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>());
        await context.IdentityService.DidNotReceiveWithAnyArgs().UpdateSecurityStampAsync(default!);
    }

    [Fact]
    public async Task RejectInvalidCodeWithoutEnablingMfa()
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
        var handler = new VerifyTwoFactorEnrollmentHandler(
            new TwoFactorActorResolver(context.StakeholderReadModelRepository, context.IdentityService),
            context.IdentityService,
            Options.Create(new TwoFactorAuthenticationOptions()),
            context.CustomTelemetryContext,
            context.SessionService,
            context.UnitOfWork);

        var result = await handler.HandleAsync(
            new VerifyTwoFactorEnrollmentCommand("000000", sessionId, actorContext), CancellationToken.None);

        result.Status.ShouldBe(VerifyTwoFactorEnrollmentStatus.InvalidCode);
        await context.IdentityService.DidNotReceiveWithAnyArgs().SetTwoFactorEnabledAsync(default!, default);
    }
}
