using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class LoginActivityReadModelRepository(AppReadDbContext dbContext) : ILoginActivityReadModelRepository
{
    public async Task<LoginActivityHistoryCursorPage> GetByStakeholderAsync(
        LoginActivityHistoryCursorRequest request,
        CancellationToken cancellationToken)
    {
        var query =
            from loginActivity in dbContext.LoginActivities.AsNoTracking()
            join ipAddress in dbContext.IpAddresses.AsNoTracking()
                on loginActivity.IpAddressId equals ipAddress.Id
            join ipAddressLocation in dbContext.IpAddressLocations.AsNoTracking()
                on loginActivity.IpAddressLocationId equals ipAddressLocation.Id into locations
            from ipAddressLocation in locations.DefaultIfEmpty()
            where loginActivity.TenantId == request.TenantId &&
                loginActivity.StakeholderId == request.StakeholderId &&
                loginActivity.ActivityType == LoginActivityType.InitialLogin
            select new
            {
                loginActivity.Id,
                loginActivity.ActivityType,
                loginActivity.OccurredAtUtc,
                IpAddress = ipAddress.Value,
                loginActivity.DeviceName,
                loginActivity.DevicePlatform,
                loginActivity.BrowserName,
                City = ipAddressLocation == null ? null : ipAddressLocation.City,
                State = ipAddressLocation == null ? null : ipAddressLocation.State,
                Country = ipAddressLocation == null ? null : ipAddressLocation.Country
            };

        if (request.CursorOccurredAtUtc.HasValue && request.CursorLoginActivityId.HasValue)
        {
            var cursorOccurredAtUtc = request.CursorOccurredAtUtc.Value;
            var cursorLoginActivityId = request.CursorLoginActivityId.Value;
            query = query.Where(activity =>
                activity.OccurredAtUtc < cursorOccurredAtUtc ||
                (activity.OccurredAtUtc == cursorOccurredAtUtc &&
                    activity.Id.CompareTo(cursorLoginActivityId) < 0));
        }

        var activities = await query
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenByDescending(activity => activity.Id)
            .Take(request.Limit + 1)
            .Select(activity => new LoginActivityHistoryReadModel(
                activity.Id,
                activity.ActivityType,
                activity.OccurredAtUtc,
                activity.IpAddress,
                activity.DeviceName,
                activity.DevicePlatform,
                activity.BrowserName,
                activity.City,
                activity.State,
                activity.Country))
            .ToListAsync(cancellationToken);

        var hasMore = activities.Count > request.Limit;
        if (hasMore)
        {
            activities.RemoveAt(activities.Count - 1);
        }

        return new LoginActivityHistoryCursorPage(activities, hasMore);
    }
}
