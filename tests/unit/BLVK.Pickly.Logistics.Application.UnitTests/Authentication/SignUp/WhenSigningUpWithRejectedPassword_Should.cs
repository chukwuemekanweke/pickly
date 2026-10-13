using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningUpWithRejectedPassword_Should
{
    [Fact]
    public async Task ReturnValidationFailure()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.WeakPassword();
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();

        var context = new AuthenticationFlowTestContext();

        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
                Description = "Passwords must have at least one digit ('0'-'9')."
            }));

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpCommand(email, password, countryId, firstName, lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.ValidationFailed);
        result.ValidationErrors.ShouldNotBeNull();
        result.ValidationErrors.ShouldContainKey(nameof(SignUpCommand.Password));
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<UserCreated>(),
            Arg.Any<CancellationToken>());
        context.CustomTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.ValidationFailed));
    }
}

