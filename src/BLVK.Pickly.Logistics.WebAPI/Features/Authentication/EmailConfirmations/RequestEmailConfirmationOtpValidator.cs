using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;

public sealed class RequestEmailConfirmationOtpValidator : AbstractValidator<RequestEmailConfirmationOtpRequest>
{
    public RequestEmailConfirmationOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
