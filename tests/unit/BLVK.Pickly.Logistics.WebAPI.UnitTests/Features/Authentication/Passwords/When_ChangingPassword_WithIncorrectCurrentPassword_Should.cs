using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Passwords;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Passwords;

public sealed class When_ChangingPassword_WithIncorrectCurrentPassword_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        var validator = Substitute.For<IValidator<ChangePasswordRequest>>();
        var request = new ChangePasswordRequest("Wr0ngP@ssword!", "N3wP@ssword!", "N3wP@ssword!");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var user = context.CreateUser();
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
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordMismatch),
                Description = "Incorrect password."
            }));
        var sut = new PasswordsController(context.CreateChangePasswordHandler(), validator, context.CurrentActor);

        var result = await sut.Change(request, CancellationToken.None);

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Value.ShouldBeOfType<ProblemDetails>().Detail.ShouldBe("Current password is incorrect");
    }
}
