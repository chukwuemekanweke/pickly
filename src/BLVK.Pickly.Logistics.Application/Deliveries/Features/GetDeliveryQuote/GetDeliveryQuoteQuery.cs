using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryQuote;

public sealed record GetDeliveryQuoteQuery(Guid QuoteId, ActorContext ActorContext);
