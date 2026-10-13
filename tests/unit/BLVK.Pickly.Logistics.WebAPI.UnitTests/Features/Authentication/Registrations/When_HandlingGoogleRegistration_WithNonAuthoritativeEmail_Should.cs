using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_HandlingGoogleRegistration_WithNonAuthoritativeEmail_Should
{
    [Fact]
    public async Task ReturnEmailVerificationContinuationMetadata()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignUpRequest>>();
        var request = new GoogleSignUpRequest("google-token", Guid.CreateVersion7(), "Jane", "Doe");
        var email = "jane@external.example";
        var tenantId = context.CurrentActor.TenantId!.Value;
        var stakeholderType = context.CreateStakeholderType();

        googleValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.GoogleAuthenticationFlowService.TakeAsync(request.FlowToken, Arg.Any<CancellationToken>())
            .Returns(new GoogleAuthenticationFlowResult(
                GoogleAuthenticationFlowStatus.Success,
                new GoogleAuthenticationFlow(
                    request.FlowToken,
                    "nonce",
                    tenantId,
                    GoogleAuthenticationFlowState.RegistrationRequired,
                    context.Clock.GetUtcNow().AddMinutes(10),
                    Guid.CreateVersion7().ToString("N"),
                    email,
                    true,
                    null)));
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(
                Arg.Any<AppUser>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(IdentityResult.Success);
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

        var result = await sut.HandleGoogle(request, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Extensions["code"].ShouldBe(AuthenticationErrorCodes.EmailVerificationRequired);
        problem.Extensions["email"].ShouldBe(email);
        problem.Extensions["retryAtUtc"].ShouldBe(
            context.Clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
    }
}
