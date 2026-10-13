using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenVerifyingOtpWithConcurrentConfirmation_Should
{
    [Fact]
    public async Task RejectSessionIssuance()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var otp = AuthenticationTestData.Otp();
        var user = context.CreateUser(email);
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName());

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(
                user.Id,
                otp,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.IdentityService.UpdateAsync(Arg.Any<AppUser>()).Returns(
            IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.ConcurrencyFailure)
            }));

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpCommand(email, otp),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.AlreadyVerified);
        result.Tokens.ShouldBeNull();
        context.AccessTokenService.DidNotReceiveWithAnyArgs().Generate(default!, default, default);
        await context.RefreshTokenService.DidNotReceiveWithAnyArgs().IssueAsync(
            default!,
            default(Guid),
            default(DateTimeOffset),
            CancellationToken.None);
    }
}
