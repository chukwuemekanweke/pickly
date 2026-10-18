using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderActiveDeliveryReader(AppDbContext dbContext) : IRiderActiveDeliveryReader
{
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<RiderAssignedDeliverySnapshot?> GetAsync(
        Guid riderStakeholderId,
        CancellationToken cancellationToken)
    {
        var delivery = await dbContext.Deliveries
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item =>
                item.RiderId == riderStakeholderId && ActiveDeliveryStatuses.Contains(item.Status),
                cancellationToken);
        return delivery is null
            ? null
            : new(
                delivery.Id,
                delivery.TrackingId,
                delivery.Status.ToString(),
                delivery.DeliveryType.ToString(),
                delivery.OrderType.ToString(),
                delivery.PaymentMethod.ToString(),
                delivery.PaymentResponsibleParty.ToString(),
                delivery.PickupAddress.AddressLine,
                delivery.PickupAddress.Latitude,
                delivery.PickupAddress.Longitude,
                delivery.DropOffAddress.AddressLine,
                delivery.DropOffAddress.Latitude,
                delivery.DropOffAddress.Longitude,
                delivery.Recipient.Name,
                delivery.Recipient.PhoneNumber,
                delivery.Items.Select(item => new RiderDeliveryItemSnapshot(item.Description, item.Quantity, item.WeightKg)).ToArray());
    }
}
