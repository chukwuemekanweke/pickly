using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class ActiveSessionReadModelRepository(AppReadDbContext dbContext) : IActiveSessionReadModelRepository
{
    public async Task<IReadOnlyList<ActiveSessionReadModel>> GetActiveByStakeholderAsync(
        Guid stakeholderId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query =
            from session in dbContext.AuthenticationSessions.AsNoTracking()
            join ipAddressLocation in dbContext.IpAddressLocations.AsNoTracking()
                on session.LastIpAddressId equals ipAddressLocation.IpAddressId into locations
            from ipAddressLocation in locations.Where(location => location.IsCurrentLocation).DefaultIfEmpty()
            where session.StakeholderId == stakeholderId &&
                session.RevokedAtUtc == null &&
                session.ExpiresAtUtc > now
            select new ActiveSessionReadModel(
                session.Id,
                session.DeviceName,
                session.DevicePlatform,
                session.BrowserName,
                session.UserAgent,
                session.FirstIpAddress.Value,
                session.LastIpAddress.Value,
                session.CreatedAtUtc,
                session.LastActiveAtUtc,
                session.ExpiresAtUtc,
                ipAddressLocation == null ? null : ipAddressLocation.City,
                ipAddressLocation == null ? null : ipAddressLocation.State,
                ipAddressLocation == null ? null : ipAddressLocation.Country);

        return await query.ToListAsync(cancellationToken);
    }
}
