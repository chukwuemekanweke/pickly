using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed class ResolveLocationRequestValidator : AbstractValidator<ResolveLocationRequest>
{
    public ResolveLocationRequestValidator()
    {
        RuleFor(request => request.AddressLine).NotEmpty().MaximumLength(LocationAddress.MaxAddressLineLength);
        RuleFor(request => request.City).NotEmpty().MaximumLength(LocationAddress.MaxLocationNameLength);
        RuleFor(request => request.State).NotEmpty().MaximumLength(LocationAddress.MaxLocationNameLength);
        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("CountryCode must be a two-letter ISO code.");
    }
}
