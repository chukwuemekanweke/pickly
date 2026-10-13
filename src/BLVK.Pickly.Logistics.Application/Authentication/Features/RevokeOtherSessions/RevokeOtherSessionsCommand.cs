namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RevokeOtherSessions;

public sealed record RevokeOtherSessionsCommand(Guid CurrentSessionId, Guid StakeholderId);
