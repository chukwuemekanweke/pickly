namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public enum CreateDeliveryStatus
{
    Success,
    NotAuthenticated,
    NotFound,
    InvalidRequest,
    QuoteExpired,
    QuoteConsumed,
    ScheduledPickupExpired,
    PersistenceConflict
}
