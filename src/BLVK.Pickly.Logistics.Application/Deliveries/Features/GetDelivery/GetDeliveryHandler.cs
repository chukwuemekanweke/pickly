using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;

public sealed class GetDeliveryHandler(
    IRepository<Delivery> deliveries,
    IRepository<Currency> currencies,
    IRiderOperationsStore? riderOperations = null)
{
    public async Task<GetDeliveryResult> HandleAsync(GetDeliveryQuery query, CancellationToken cancellationToken)
    {
        if (!query.ActorContext.StakeholderId.HasValue || query.ActorContext.StakeholderId == Guid.Empty)
        {
            return new(GetDeliveryStatus.NotAuthenticated);
        }

        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndIdSpecification(query.ActorContext.StakeholderId.Value, query.DeliveryId),
            cancellationToken);
        if (delivery is null)
        {
            return new(GetDeliveryStatus.NotFound);
        }

        var currency = await currencies.GetByIdAsync(delivery.Charge.CurrencyId, cancellationToken);
        var assignment = riderOperations is null
            ? null
            : await riderOperations.GetSenderAssignmentAsync(delivery.Id, cancellationToken);
        return new(
            GetDeliveryStatus.Success,
            DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty, assignment));
    }
}
