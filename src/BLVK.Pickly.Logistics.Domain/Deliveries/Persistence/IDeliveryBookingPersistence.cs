using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;

public interface IDeliveryBookingPersistence
{
    Task<DeliveryBookingCommitResult> CommitAsync(
        Delivery delivery,
        DeliveryQuote quote,
        DeliveryBookingConfirmation confirmation,
        CancellationToken cancellationToken = default);
}

public sealed record DeliveryBookingCommitResult(
    DeliveryBookingCommitStatus Status,
    Guid? ExistingDeliveryId = null,
    string? ExistingRequestFingerprint = null);

public enum DeliveryBookingCommitStatus
{
    Created,
    Existing,
    Conflict
}
