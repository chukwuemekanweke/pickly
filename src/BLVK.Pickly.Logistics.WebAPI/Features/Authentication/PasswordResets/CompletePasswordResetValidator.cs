using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;

public sealed class CompletePasswordResetValidator : AbstractValidator<CompletePasswordResetRequest>
{
    public CompletePasswordResetValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Otp)
            .NotEmpty()
            .Length(6)
            .Matches("^[a-zA-Z0-9]{6}$");

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
    }
}
