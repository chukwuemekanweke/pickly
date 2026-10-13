using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;

public sealed record GetLoginActivityHistoryCommand(
    int Limit,
    string? Cursor,
    ActorContext ActorContext);
