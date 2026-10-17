using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public sealed record CreateDeliveryCommand(
    Guid QuoteId,
    string RecipientName,
    string RecipientPhoneNumber,
    string? DropOffInstructions,
    ActorContext ActorContext);
