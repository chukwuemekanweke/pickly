namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record DeliveryQuoteItemInput(string Description, int Quantity, decimal WeightKg);
