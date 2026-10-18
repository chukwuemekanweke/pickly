namespace BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;

public interface IDeliveryPaymentInitiationConsistencyLock
{
    Task AcquireAsync(Guid deliveryId, CancellationToken cancellationToken = default);
}
