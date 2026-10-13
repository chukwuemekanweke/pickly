namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record RefreshSessionCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent,
    ActorContext ActorContext);
