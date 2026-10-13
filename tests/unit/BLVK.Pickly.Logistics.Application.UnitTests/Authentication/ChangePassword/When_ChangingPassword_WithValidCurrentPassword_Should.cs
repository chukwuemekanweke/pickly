using BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.ChangePassword;

public sealed class When_ChangingPassword_WithValidCurrentPassword_Should
{
    [Fact]
    public async Task ChangePassword()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateChangePasswordCommand();
        var user = context.CreateUser();
        var stakeholderId = command.ActorContext.StakeholderId!.Value;
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(
                stakeholderId,
                Arg.Any<CancellationToken>())
            .Returns(CreateStakeholder(stakeholderId, user.Id, command.ActorContext.TenantId!.Value, user.Email!));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword)
            .Returns(IdentityResult.Success);

        var result = await context.CreateChangePasswordHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ChangePasswordStatus.Success);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static StakeholderReadModel CreateStakeholder(
        Guid stakeholderId,
        Guid appUserId,
        Guid tenantId,
        string email) =>
        new(
            stakeholderId,
            appUserId,
            email,
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe",
            null,
            true);
}
