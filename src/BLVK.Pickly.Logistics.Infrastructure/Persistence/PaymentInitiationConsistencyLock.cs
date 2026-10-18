using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class PaymentInitiationConsistencyLock(AppDbContext dbContext) : IDeliveryPaymentInitiationConsistencyLock
{
    public Task AcquireAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var lockKey = $"delivery-payment:{deliveryId:N}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }
}
