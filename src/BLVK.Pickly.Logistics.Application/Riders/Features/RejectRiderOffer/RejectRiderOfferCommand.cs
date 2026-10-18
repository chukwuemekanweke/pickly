using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;

public sealed record RejectRiderOfferCommand(Guid OfferId, ActorContext ActorContext);
