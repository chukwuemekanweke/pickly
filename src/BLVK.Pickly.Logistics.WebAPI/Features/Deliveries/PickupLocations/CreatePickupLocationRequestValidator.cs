using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed class CreatePickupLocationRequestValidator : AbstractValidator<CreatePickupLocationRequest>
{
    public CreatePickupLocationRequestValidator()
    {
        RuleFor(request => request.Label).NotEmpty().MaximumLength(SavedPickupLocation.MaxLabelLength);
        RuleFor(request => request.Address)
            .NotNull()
            .SetValidator(new PickupLocationAddressRequestValidator());
    }
}
