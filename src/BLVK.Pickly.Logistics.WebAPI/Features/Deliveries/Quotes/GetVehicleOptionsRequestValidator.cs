using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using FluentValidation;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

public sealed class GetVehicleOptionsRequestValidator : AbstractValidator<GetVehicleOptionsRequest>
{
    public GetVehicleOptionsRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.PickupLocationId).NotEmpty();
        RuleFor(x => x.DropOffLocationId).NotEmpty();
        RuleFor(x => x.DeliveryType).Must(x => Enum.TryParse<DeliveryType>(x, true, out _)).WithMessage("DeliveryType must be Instant or Scheduled.");
        RuleFor(x => x.OrderType).Must(x => Enum.TryParse<DeliveryOrderType>(x, true, out _)).WithMessage("OrderType must be PayBeforeDelivery or PayOnDelivery.");
        RuleFor(x => x.ScheduledPickupAtUtc).NotNull().GreaterThan(timeProvider.GetUtcNow()).When(x => string.Equals(x.DeliveryType, nameof(DeliveryType.Scheduled), StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.ScheduledPickupAtUtc).Null().When(x => string.Equals(x.DeliveryType, nameof(DeliveryType.Instant), StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Items).NotEmpty().Must(x => x is { Count: <= 100 }).WithMessage("No more than 100 parcel lines are allowed.");
        RuleForEach(x => x.Items).ChildRules(item => { item.RuleFor(x => x.Description).NotEmpty().MaximumLength(500); item.RuleFor(x => x.Quantity).GreaterThan(0); item.RuleFor(x => x.WeightKg).GreaterThan(0m).PrecisionScale(12, 3, false); });
    }
}
