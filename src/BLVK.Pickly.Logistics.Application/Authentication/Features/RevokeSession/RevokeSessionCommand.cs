namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId, Guid StakeholderId);
