using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryQuote;

public sealed record GetDeliveryQuoteResult(GetDeliveryQuoteStatus Status, DeliveryQuoteResponse? Quote = null);
