namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Bookings;

public sealed record CreateDeliveryRequest(
    Guid QuoteId,
    DeliveryRecipientRequest Recipient,
    string? DropOffInstructions);

public sealed record DeliveryRecipientRequest(string Name, string PhoneNumber);
