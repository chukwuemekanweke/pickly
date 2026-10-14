using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Localization;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed class GoogleSignUpValidator : AbstractValidator<GoogleSignUpRequest>
{
    public GoogleSignUpValidator(IRegistrationPolicyProvider registrationPolicyProvider)
    {
        RuleFor(request => request.FlowToken)
            .NotEmpty();

        RuleFor(request => request.FirstName)
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .MaximumLength(100);

        RuleFor(request => request.Language)
            .Must(language => string.IsNullOrWhiteSpace(language) || SupportedLanguages.IsSupported(language.Trim()))
            .WithMessage("Language is not supported.");

        WhenAsync(
            async (_, _) => (await registrationPolicyProvider.GetAsync()).RequireProfileDetails,
            () =>
            {
                RuleFor(request => request.CountryId)
                    .NotEmpty();

                RuleFor(request => request.FirstName)
                    .NotEmpty();

                RuleFor(request => request.LastName)
                    .NotEmpty();

                RuleFor(request => request.Language)
                    .NotEmpty();
            });
    }
}
