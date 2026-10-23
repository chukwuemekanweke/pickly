using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class DeviceAccountLock(AppDbContext dbContext) : IDeviceAccountLock
{
    public Task AcquireAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = DeviceAccount.NormalizeIdentifier(identifier);
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({normalizedIdentifier}, 0))",
            cancellationToken);
    }
}
