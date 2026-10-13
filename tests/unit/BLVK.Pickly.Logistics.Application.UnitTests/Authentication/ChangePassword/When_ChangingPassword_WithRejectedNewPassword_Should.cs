using BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.ChangePassword;

public sealed class When_ChangingPassword_WithRejectedNewPassword_Should
{
    [Fact]
    public async Task ReturnPolicyErrors()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateChangePasswordCommand(newPassword: "weak");
        var user = context.CreateUser();
        var stakeholderId = command.ActorContext.StakeholderId!.Value;
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(
                stakeholderId,
                Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                user.Id,
                user.Email!,
                command.ActorContext.TenantId!.Value,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Jane",
                "Doe",
                null,
                true));
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword)
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordTooShort),
                Description = "Passwords must be at least 8 characters."
            }));

        var result = await context.CreateChangePasswordHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(ChangePasswordStatus.ValidationFailed);
        result.ValidationErrors.ShouldNotBeNull();
        result.ValidationErrors.ShouldContainKey(nameof(ChangePasswordCommand.NewPassword));
    }
}
