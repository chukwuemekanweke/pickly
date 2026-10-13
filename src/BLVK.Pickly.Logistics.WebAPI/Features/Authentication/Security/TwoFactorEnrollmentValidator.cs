using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;

public sealed class TwoFactorEnrollmentValidator : AbstractValidator<TwoFactorEnrollmentRequest>
{
    public TwoFactorEnrollmentValidator() =>
        RuleFor(request => request.Code).Matches("^[0-9]{6}$");
}
