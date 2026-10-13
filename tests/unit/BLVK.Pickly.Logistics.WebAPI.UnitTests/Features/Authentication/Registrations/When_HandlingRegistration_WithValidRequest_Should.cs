using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_HandlingRegistration_WithValidRequest_Should
{
    [Fact]
    public async Task AcceptRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignUpRequest>>();
        var request = new SignUpRequest(
            "jane@example.com",
            "P@ssw0rd123!",
            "P@ssw0rd123!",
            Guid.CreateVersion7(),
            "Jane",
            "Doe");

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        var stakeholderType = context.CreateStakeholderType();
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var sut = new RegistrationsController(
            context.CreateSignUpHandler(),
            context.CreateGoogleSignUpHandler(),
            validator,
            googleValidator,
            context.CurrentActor);

        var result = await sut.Handle(request, CancellationToken.None);

        var accepted = result.Result.ShouldBeOfType<AcceptedResult>();
        var response = accepted.Value.ShouldBeOfType<SignUpResponse>();
        response.Email.ShouldBe(request.Email);
        response.RetryAtUtc.ShouldBe(context.Clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
    }
}
