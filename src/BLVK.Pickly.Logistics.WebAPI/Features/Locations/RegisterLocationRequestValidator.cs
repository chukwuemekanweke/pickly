using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed class RegisterLocationRequestValidator : AbstractValidator<RegisterLocationRequest>
{
    public RegisterLocationRequestValidator()
    {
        RuleFor(request => request.SelectionToken).NotEmpty().MaximumLength(64);
    }
}
