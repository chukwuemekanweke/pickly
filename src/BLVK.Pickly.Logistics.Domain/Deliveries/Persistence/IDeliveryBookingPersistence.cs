using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;

public interface IDeliveryBookingPersistence
{
    Task<DeliveryBookingCommitResult> CommitAsync(
        Delivery delivery,
        DeliveryQuote quote,
        CancellationToken cancellationToken = default);
}

public sealed record DeliveryBookingCommitResult(DeliveryBookingCommitStatus Status);

public enum DeliveryBookingCommitStatus
{
    Created,
    Existing,
    Conflict
}
