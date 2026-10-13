namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;

public sealed record ListSessionsQuery(Guid StakeholderId, Guid CurrentSessionId);
