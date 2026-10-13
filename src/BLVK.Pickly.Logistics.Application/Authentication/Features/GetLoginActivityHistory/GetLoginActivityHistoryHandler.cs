using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;

public sealed class GetLoginActivityHistoryHandler(ILoginActivityReadModelRepository loginActivityReadModelRepository)
{
    public async Task<GetLoginActivityHistoryResult> HandleAsync(
        GetLoginActivityHistoryCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        var tenantId = command.ActorContext.TenantId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty ||
            !tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return new GetLoginActivityHistoryResult(
                GetLoginActivityHistoryStatus.NotAuthenticated,
                [],
                null);
        }

        var (cursorOccurredAtUtc, cursorLoginActivityId) = CursorPagination.Decode(command.Cursor);
        var page = await loginActivityReadModelRepository.GetByStakeholderAsync(
            new LoginActivityHistoryCursorRequest(
                tenantId.Value,
                stakeholderId.Value,
                cursorOccurredAtUtc,
                cursorLoginActivityId,
                command.Limit),
            cancellationToken);

        var activities = page.Activities
            .Select(activity => new GetLoginActivityHistoryResponse(
                activity.LoginActivityId,
                activity.ActivityType.ToString(),
                activity.OccurredAtUtc,
                activity.IpAddress,
                activity.DeviceName,
                activity.DevicePlatform,
                activity.BrowserName,
                activity.City,
                activity.State,
                activity.Country))
            .ToArray();

        var nextCursor = page.HasMore && page.Activities.Count > 0
            ? CursorPagination.Encode(
                page.Activities[^1].OccurredAtUtc,
                page.Activities[^1].LoginActivityId)
            : null;

        return new GetLoginActivityHistoryResult(
            GetLoginActivityHistoryStatus.Success,
            activities,
            nextCursor);
    }
}
