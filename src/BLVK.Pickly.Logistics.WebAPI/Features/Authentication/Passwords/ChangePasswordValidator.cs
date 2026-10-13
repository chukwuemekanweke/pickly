using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Passwords;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("NewPassword must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("NewPassword must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("NewPassword must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("NewPassword must contain at least one non-alphanumeric character.")
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("NewPassword must be different from CurrentPassword.");

        RuleFor(request => request.ConfirmNewPassword)
            .NotEmpty()
            .Equal(request => request.NewPassword)
            .WithMessage("NewPassword and ConfirmNewPassword must match.");
    }
}
