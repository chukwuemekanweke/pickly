namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public enum CreateDeliveryStatus
{
    Success,
    NotAuthenticated,
    NotFound,
    InvalidRequest,
    InvalidIdempotencyKey,
    QuoteExpired,
    QuoteConsumed,
    ScheduledPickupExpired,
    IdempotencyConflict,
    PersistenceConflict
}
