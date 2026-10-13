using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;

public sealed record LoginActivityHistoryResponse(
    IReadOnlyList<GetLoginActivityHistoryResponse> Activities,
    string? NextCursor);
