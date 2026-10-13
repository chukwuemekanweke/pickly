using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Passwords;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Passwords;

public sealed class When_ChangingPassword_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnNoContent()
    {
        var context = new AuthenticationControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        var validator = Substitute.For<IValidator<ChangePasswordRequest>>();
        var request = new ChangePasswordRequest("P@ssw0rd123!", "N3wP@ssword!", "N3wP@ssword!");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var user = context.CreateUser();
        ConfigureStakeholder(context, stakeholderId, user);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)
            .Returns(IdentityResult.Success);
        var sut = new PasswordsController(context.CreateChangePasswordHandler(), validator, context.CurrentActor);

        var result = await sut.Change(request, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }

    private static void ConfigureStakeholder(
        AuthenticationControllerTestContext context,
        Guid stakeholderId,
        AppUser user)
    {
        var tenantId = context.CurrentActor.TenantId!.Value;
        context.StakeholderReadModelRepository.GetByStakeholderIdAsync(
                stakeholderId,
                Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                user.Id,
                user.Email!,
                tenantId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "Jane",
                "Doe",
                null,
                true));
    }
}
