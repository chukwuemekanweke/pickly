using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed class SignInValidator : AbstractValidator<SignInRequest>
{
    public SignInValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
