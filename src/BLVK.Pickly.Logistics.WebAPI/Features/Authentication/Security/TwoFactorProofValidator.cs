using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;

public sealed class TwoFactorProofValidator : AbstractValidator<TwoFactorProofRequest>
{
    public TwoFactorProofValidator()
    {
        RuleFor(request => request.VerificationMethod)
            .Must(method => method is "authenticator" or "recovery_code")
            .WithMessage("Verification method must be 'authenticator' or 'recovery_code'.");
        RuleFor(request => request.Code).NotEmpty().MaximumLength(128);
    }
}
