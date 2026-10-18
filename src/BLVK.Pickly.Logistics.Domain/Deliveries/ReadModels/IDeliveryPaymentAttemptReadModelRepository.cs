namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public interface IDeliveryPaymentAttemptReadModelRepository
{
    Task<IReadOnlyList<DeliveryPaymentAttemptReadModel>> ListForDecisionAsync(
        Guid deliveryId,
        CancellationToken cancellationToken);

    Task<DeliveryPaymentAttemptReadModel?> GetForDecisionAsync(
        Guid paymentTransactionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliveryPaymentAttemptReadModel>> ListHistoryAsync(
        Guid deliveryId,
        CancellationToken cancellationToken);
}
