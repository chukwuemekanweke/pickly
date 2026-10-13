namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public sealed record LoginActivityHistoryCursorPage(
    IReadOnlyList<LoginActivityHistoryReadModel> Activities,
    bool HasMore);
