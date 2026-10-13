using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;

public sealed class SignUpOtpValidator : AbstractValidator<SignUpOtpRequest>
{
    public SignUpOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Otp)
            .NotEmpty()
            .Length(6);
    }
}
