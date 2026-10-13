using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;

public sealed class ListSessionsHandler(
    IActiveSessionReadModelRepository activeSessionReadModelRepository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ActiveSessionResult>> HandleAsync(
        ListSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await activeSessionReadModelRepository.GetActiveByStakeholderAsync(
            query.StakeholderId, timeProvider.GetUtcNow(), cancellationToken);
        return sessions
            .OrderByDescending(session => session.SessionId == query.CurrentSessionId)
            .ThenByDescending(session => session.LastActiveAtUtc)
            .Select(session => new ActiveSessionResult(session.SessionId, session.DeviceName,
                session.DevicePlatform, session.BrowserName, session.UserAgent,
                session.FirstIpAddress, session.LastIpAddress,
                session.CreatedAtUtc, session.LastActiveAtUtc, session.ExpiresAtUtc,
                session.SessionId == query.CurrentSessionId,
                session.City, session.State, session.Country)).ToArray();
    }
}
