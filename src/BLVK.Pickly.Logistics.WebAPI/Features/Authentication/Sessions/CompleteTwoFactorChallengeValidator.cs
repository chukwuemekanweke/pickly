using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed class CompleteTwoFactorChallengeValidator : AbstractValidator<CompleteTwoFactorChallengeRequest>
{
    public CompleteTwoFactorChallengeValidator()
    {
        RuleFor(request => request.Challenge).NotEmpty().MaximumLength(256);
        RuleFor(request => request.VerificationMethod)
            .Must(method => method is "authenticator" or "recovery_code")
            .WithMessage("Verification method must be 'authenticator' or 'recovery_code'.");
        RuleFor(request => request.Code).NotEmpty().MaximumLength(128);
    }
}
