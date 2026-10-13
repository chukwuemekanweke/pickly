namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public sealed record LoginActivityHistoryCursorRequest(
    Guid TenantId,
    Guid StakeholderId,
    DateTimeOffset? CursorOccurredAtUtc,
    Guid? CursorLoginActivityId,
    int Limit);
