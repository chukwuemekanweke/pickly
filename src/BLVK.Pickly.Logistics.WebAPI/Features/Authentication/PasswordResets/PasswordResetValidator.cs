using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;

public sealed class PasswordResetValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
