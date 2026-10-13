namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;

public sealed record GetLoginActivityHistoryResult(
    GetLoginActivityHistoryStatus Status,
    IReadOnlyList<GetLoginActivityHistoryResponse> Activities,
    string? NextCursor);

public enum GetLoginActivityHistoryStatus
{
    Success = 1,
    NotAuthenticated = 2
}
