using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailExistenceChecks;

public sealed class EmailExistenceCheckValidator : AbstractValidator<EmailExistenceCheckRequest>
{
    public EmailExistenceCheckValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
