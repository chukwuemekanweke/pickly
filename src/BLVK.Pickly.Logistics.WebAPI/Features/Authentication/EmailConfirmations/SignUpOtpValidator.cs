using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;

public sealed class SignUpOtpValidator : AbstractValidator<SignUpOtpRequest>
{
    public SignUpOtpValidator(
        IRegistrationPolicyProvider registrationPolicyProvider,
        IAuthenticationIdentityService identityService,
        ITwoFactorOtpService twoFactorOtpService)
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Otp)
            .NotEmpty()
            .CustomAsync(async (otp, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(otp))
                {
                    return;
                }

                var policy = await registrationPolicyProvider.GetAsync();
                var expectedLength = policy.EmailConfirmationOtpLength;
                var email = context.InstanceToValidate.Email;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = await identityService.FindByEmailAsync(email.Trim().ToLowerInvariant());
                    if (user is not null)
                    {
                        var activeOtp = await twoFactorOtpService.GetActiveOtpAsync(
                            user.Id,
                            OtpIntent.EmailConfirmation,
                            cancellationToken);
                        expectedLength = activeOtp?.Code.Length ?? expectedLength;
                    }
                }

                if (otp.Length != expectedLength || !otp.All(char.IsDigit))
                {
                    context.AddFailure($"OTP must contain {expectedLength} numeric digits.");
                }
            });
    }
}
