using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed record AcceptRiderOfferCommand(Guid OfferId, ActorContext ActorContext);
