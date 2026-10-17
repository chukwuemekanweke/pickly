using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryBookingByKeySpecification : Specification<DeliveryBookingConfirmation>
{
    public DeliveryBookingByKeySpecification(Guid senderId, string keyHash) =>
        Where(confirmation =>
            confirmation.SenderId == senderId &&
            confirmation.Operation == DeliveryBookingConfirmation.OperationName &&
            confirmation.KeyHash == keyHash);
}
