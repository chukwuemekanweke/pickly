using BLVK.Pickly.Logistics.Domain.Common.Localization;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed class GoogleSignUpValidator : AbstractValidator<GoogleSignUpRequest>
{
    public GoogleSignUpValidator()
    {
        RuleFor(request => request.FlowToken)
            .NotEmpty();

        RuleFor(request => request.CountryId)
            .NotEmpty();

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Language)
            .Must(SupportedLanguages.IsSupported)
            .WithMessage("Language is not supported.");
    }
}
