using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed class GoogleSignInValidator : AbstractValidator<GoogleSignInRequest>
{
    public GoogleSignInValidator()
    {
        RuleFor(request => request.FlowToken)
            .NotEmpty();

        RuleFor(request => request.IdToken)
            .NotEmpty();
    }
}
