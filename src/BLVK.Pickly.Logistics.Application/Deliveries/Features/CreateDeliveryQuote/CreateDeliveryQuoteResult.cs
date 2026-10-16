using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDeliveryQuote;

public sealed record CreateDeliveryQuoteResult(DeliveryQuotePreparationStatus Status, DeliveryQuoteResponse? Quote = null);
