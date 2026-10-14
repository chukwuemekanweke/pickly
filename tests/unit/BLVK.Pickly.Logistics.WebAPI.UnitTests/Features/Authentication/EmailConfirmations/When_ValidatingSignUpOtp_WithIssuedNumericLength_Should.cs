using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.EmailConfirmations;

public sealed class When_ValidatingSignUpOtp_WithIssuedNumericLength_Should
{
    [Theory]
    [InlineData(true, null, "12345", true)]
    [InlineData(true, null, "123456", false)]
    [InlineData(false, null, "123456", true)]
    [InlineData(false, null, "12345", false)]
    [InlineData(true, 6, "123456", true)]
    [InlineData(false, 5, "12345", true)]
    public async Task ApplyPolicyWithoutInvalidatingActiveOtp(
        bool isMvp,
        int? activeOtpLength,
        string otp,
        bool expectedValid)
    {
        const string Email = "jane@example.com";
        var user = AppUser.Create(Email);
        var policyProvider = Substitute.For<IRegistrationPolicyProvider>();
        var identityService = Substitute.For<IAuthenticationIdentityService>();
        var otpService = Substitute.For<ITwoFactorOtpService>();
        policyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp));
        identityService.FindByEmailAsync(Email).Returns(user);
        otpService.GetActiveOtpAsync(user.Id, OtpIntent.EmailConfirmation, Arg.Any<CancellationToken>())
            .Returns(activeOtpLength.HasValue
                ? new TwoFactorOtp(
                    new string('1', activeOtpLength.Value),
                    new DateTimeOffset(2026, 10, 14, 12, 1, 0, TimeSpan.Zero))
                : null);
        var validator = new SignUpOtpValidator(policyProvider, identityService, otpService);

        var result = await validator.ValidateAsync(new SignUpOtpRequest(Email, otp));

        result.IsValid.ShouldBe(expectedValid);
    }
}
