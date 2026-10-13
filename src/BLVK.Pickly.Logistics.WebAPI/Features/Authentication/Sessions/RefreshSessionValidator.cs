using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionRequest>
{
    public RefreshSessionValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
