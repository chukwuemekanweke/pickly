using BLVK.Pickly.Logistics.Domain.Common.Localization;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;

public sealed class SignUpValidator : AbstractValidator<SignUpRequest>
{
    public SignUpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(request => request.ConfirmPassword)
            .NotEmpty()
            .Equal(request => request.Password)
            .WithMessage("Password and ConfirmPassword must match.");

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
