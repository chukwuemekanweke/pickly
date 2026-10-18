using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

public sealed record DeliveryResponse(
    Guid DeliveryId,
    Guid QuoteId,
    string TrackingId,
    string DeliveryType,
    string OrderType,
    string Status,
    string PaymentStatus,
    string PaymentResponsibleParty,
    string PaymentMethod,
    Guid VehicleCategoryId,
    DeliveryChargeResponse Charge,
    DeliveryRecipientResponse Recipient,
    DeliveryAddressResponse PickupAddress,
    DeliveryAddressResponse DropOffAddress,
    IReadOnlyCollection<DeliveryItemResponse> Items,
    DateTimeOffset? ScheduledPickupAtUtc,
    DateTimeOffset? DispatchEligibleAtUtc)
{
    public static DeliveryResponse From(Delivery delivery, string currencyCode) =>
        new(
            delivery.Id,
            delivery.DeliveryQuoteId,
            delivery.TrackingId,
            delivery.DeliveryType.ToString(),
            delivery.OrderType.ToString(),
            delivery.Status.ToString(),
            delivery.PaymentStatus.ToString(),
            delivery.PaymentResponsibleParty.ToString(),
            delivery.PaymentMethod.ToString(),
            delivery.VehicleCategoryId,
            new(delivery.Charge.Amount, currencyCode),
            new(delivery.Recipient.Name, delivery.Recipient.PhoneNumber),
            DeliveryAddressResponse.From(delivery.PickupAddress),
            DeliveryAddressResponse.From(delivery.DropOffAddress),
            delivery.Items.Select(item => new DeliveryItemResponse(item.Description, item.Quantity, item.WeightKg)).ToArray(),
            delivery.ScheduledPickupAtUtc,
            delivery.DispatchEligibleAtUtc);
}

public sealed record DeliveryChargeResponse(decimal Amount, string CurrencyCode);
public sealed record DeliveryRecipientResponse(string Name, string PhoneNumber);
public sealed record DeliveryItemResponse(string Description, int Quantity, decimal WeightKg);
