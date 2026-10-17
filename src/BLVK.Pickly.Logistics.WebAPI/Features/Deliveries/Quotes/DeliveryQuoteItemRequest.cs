namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

public sealed record DeliveryQuoteItemRequest(string Description, int Quantity, decimal WeightKg);
