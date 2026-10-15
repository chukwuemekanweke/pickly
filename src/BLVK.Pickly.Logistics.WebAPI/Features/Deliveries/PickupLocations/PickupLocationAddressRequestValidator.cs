using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed class PickupLocationAddressRequestValidator : AbstractValidator<PickupLocationAddressRequest>
{
    public PickupLocationAddressRequestValidator()
    {
        RuleFor(request => request.AddressLine).NotEmpty().MaximumLength(300);
        RuleFor(request => request.AdditionalInformation).MaximumLength(500);
        RuleFor(request => request.City).NotEmpty().MaximumLength(100);
        RuleFor(request => request.State).NotEmpty().MaximumLength(100);
        RuleFor(request => request.CountryCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("CountryCode must be a two-letter ISO code.");
        RuleFor(request => request.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(request => request.Longitude).InclusiveBetween(-180m, 180m);
    }
}
