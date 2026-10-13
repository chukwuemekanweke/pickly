namespace BLVK.Pickly.Logistics.Application.Authentication.Features.LogoutSession;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record LogoutSessionCommand(
    string TokenId,
    DateTimeOffset ExpiresAtUtc,
    Guid SessionId,
    Guid StakeholderId,
    ActorContext ActorContext);
