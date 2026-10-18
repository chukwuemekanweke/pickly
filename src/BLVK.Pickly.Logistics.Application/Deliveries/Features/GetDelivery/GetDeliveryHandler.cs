using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;

public sealed class GetDeliveryHandler(
    IRepository<Delivery> deliveries,
    IRepository<Currency> currencies,
    ISenderDeliveryAssignmentReader assignments)
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
        var assignment = await assignments.GetAsync(delivery.Id, cancellationToken);
        return new(
            GetDeliveryStatus.Success,
            DeliveryResponse.From(delivery, currency?.CurrencyCode ?? string.Empty, assignment));
    }
}
